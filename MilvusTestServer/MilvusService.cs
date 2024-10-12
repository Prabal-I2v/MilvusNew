using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Milvus.Client;
using MilvusTestServer;
using Newtonsoft.Json;
using Enum = System.Enum;

public class MilvusService
{
    private MilvusClient _milvusClient;
    private readonly HttpClient _httpClient;

    public MilvusService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ConnectToMilvusAsync(string ip, int port)
    {
        try
        {
            _milvusClient = new MilvusClient(ip, port);
            var data = await _milvusClient.HealthAsync();
            return data.IsHealthy;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<bool> CreateCollectionAsync(string collectionName, int dimension, bool isDynamicSchema = false)
    {
        try
        {
            var schema = new CollectionSchema();
            schema.Name = collectionName;
            schema.EnableDynamicFields = isDynamicSchema;
            schema.Fields.Add(FieldSchema.Create("id", MilvusDataType.Int64, isPrimaryKey: true, autoId: true));
            schema.Fields.Add(FieldSchema.CreateFloatVector("embeddings", dimension: dimension,
                "Embeddings of the event"));
            await _milvusClient.CreateCollectionAsync(collectionName, schema, ConsistencyLevel.Strong);
            return true;
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<MutationResult> InsertDataAsync(string collectionName, List<InsertDataRequest> milvusData)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var fieldsData = new List<FieldData>();
                var DataFields = new Dictionary<string, List<MilvusDataType>>();

                var allFieldsInfo = milvusData
                    .SelectMany(d => d.Fields.Select(f => new { f.Name, f.DataType })) // Fetch both Name and DataType
                    .Distinct()
                    .ToList();

// Create a dictionary to hold field values with their types
                var DataFieldsWithType = new Dictionary<string, object>();

// Initialize the dictionary with typed lists based on DataType
                foreach (var fieldInfo in allFieldsInfo)
                {
                    switch (fieldInfo.DataType)
                    {
                        case MilvusDataType.Bool:
                            DataFieldsWithType[fieldInfo.Name] = new List<bool>();
                            break;
                        case MilvusDataType.Int8:
                        case MilvusDataType.Int16:
                        case MilvusDataType.Int32:
                        case MilvusDataType.Int64:
                            DataFieldsWithType[fieldInfo.Name] = new List<long>(); // Use long for all integer types
                            break;
                        case MilvusDataType.Float:
                            DataFieldsWithType[fieldInfo.Name] = new List<float>();
                            break;
                        case MilvusDataType.Double:
                            DataFieldsWithType[fieldInfo.Name] = new List<double>();
                            break;
                        case MilvusDataType.String:
                        case MilvusDataType.VarChar:
                            DataFieldsWithType[fieldInfo.Name] = new List<string>();
                            break;
                        case MilvusDataType.BinaryVector:
                            DataFieldsWithType[fieldInfo.Name] =
                                new List<byte[]>(); // Assuming binary vectors are byte arrays
                            break;
                        case MilvusDataType.FloatVector:
                            DataFieldsWithType[fieldInfo.Name] =
                                new List<List<float>>(); // Assuming float vectors are arrays of floats
                            break;
                        default:
                            DataFieldsWithType[fieldInfo.Name] =
                                new List<object>(); // Fallback to object for unhandled types
                            break;
                    }
                }

                // Populate the lists
                foreach (var data in milvusData)
                {
                    foreach (var fieldInfo in allFieldsInfo)
                    {
                        var fieldData = data.Fields.FirstOrDefault(f => f.Name == fieldInfo.Name);

                        if (fieldData != null)
                        {
                            // Add value to the correct list based on its type
                            switch (fieldInfo.DataType)
                            {
                                case MilvusDataType.Bool:
                                    var list = (List<bool>)DataFieldsWithType[fieldInfo.Name];
                                    list.Add(fieldData.Value);
                                    break;
                                case MilvusDataType.Int8:
                                case MilvusDataType.Int16:
                                case MilvusDataType.Int32:
                                case MilvusDataType.Int64:
                                    var list1 = (List<long>)DataFieldsWithType[fieldInfo.Name];
                                    list1.Add(
                                        Convert.ToInt64(fieldData.Value));
                                    break;
                                case MilvusDataType.Float:
                                    var list2 = (List<float>)DataFieldsWithType[fieldInfo.Name];
                                    list2.Add(
                                        Convert.ToSingle(fieldData.Value));
                                    break;
                                case MilvusDataType.Double:
                                    var list3 = (List<float>)DataFieldsWithType[fieldInfo.Name];
                                    list3.Add(
                                        Convert.ToDouble(fieldData.Value));
                                    break;
                                case MilvusDataType.String:
                                case MilvusDataType.VarChar:
                                    var list4 = (List<string>)DataFieldsWithType[fieldInfo.Name];
                                    list4.Add(fieldData.Value.ToString());
                                    break;
                                case MilvusDataType.BinaryVector:
                                    var list5 = (List<byte>)DataFieldsWithType[fieldInfo.Name];
                                    var res1 = JsonConvert.DeserializeObject<List<float>>(fieldData.Value);
                                    list5.Add(res1);
                                    break;
                                case MilvusDataType.FloatVector:
                                    var list6 = (List<List<float>>)DataFieldsWithType[fieldInfo.Name];
                                    var res2 = JsonConvert.DeserializeObject<List<float>>(fieldData.Value.ToString());
                                    list6.Add(res2);
                                    break;
                                default:
                                    ((List<object>)DataFieldsWithType[fieldInfo.Name])
                                        .Add(fieldData.Value); // Fallback for unhandled types
                                    break;
                            }
                        }
                        else
                        {
                            // If field is missing, add a default value (null for reference types)
                            switch (fieldInfo.DataType)
                            {
                                case MilvusDataType.Bool:
                                    ((List<bool>)DataFieldsWithType[fieldInfo.Name]).Add(default(bool));
                                    break;
                                case MilvusDataType.Int8:
                                case MilvusDataType.Int16:
                                case MilvusDataType.Int32:
                                case MilvusDataType.Int64:
                                    ((List<long>)DataFieldsWithType[fieldInfo.Name]).Add(default(long));
                                    break;
                                case MilvusDataType.Float:
                                    ((List<float>)DataFieldsWithType[fieldInfo.Name]).Add(default(float));
                                    break;
                                case MilvusDataType.Double:
                                    ((List<double>)DataFieldsWithType[fieldInfo.Name]).Add(default(double));
                                    break;
                                case MilvusDataType.String:
                                case MilvusDataType.VarChar:
                                    ((List<string>)DataFieldsWithType[fieldInfo.Name]).Add(null);
                                    break;
                                case MilvusDataType.BinaryVector:
                                    ((List<byte[]>)DataFieldsWithType[fieldInfo.Name]).Add(null);
                                    break;
                                case MilvusDataType.FloatVector:
                                    ((List<List<float>>)DataFieldsWithType[fieldInfo.Name]).Add(null);
                                    break;
                                default:
                                    ((List<object>)DataFieldsWithType[fieldInfo.Name]).Add(null);
                                    break;
                            }
                        }
                    }
                }

                // Example of how to use the data (modify as per your requirement)
                foreach (var kvp in DataFieldsWithType)
                {
                    var fieldName = kvp.Key;
                    var fieldValues = kvp.Value;
                    // Determine the type of data and call the appropriate Create method
                    var dataType = allFieldsInfo.First(f => f.Name == fieldName).DataType;

                    switch (dataType)
                    {
                        case MilvusDataType.Bool:
                            fieldsData.Add(FieldData.Create(fieldName, (List<bool>)fieldValues));
                            break;
                        case MilvusDataType.Int8:
                        case MilvusDataType.Int16:
                        case MilvusDataType.Int32:
                        case MilvusDataType.Int64:
                            fieldsData.Add(FieldData.Create(fieldName,
                                (List<long>)fieldValues)); // Using long for integer types
                            break;
                        case MilvusDataType.Float:
                            fieldsData.Add(FieldData.Create(fieldName, (List<float>)fieldValues));
                            break;
                        case MilvusDataType.Double:
                            fieldsData.Add(FieldData.Create(fieldName, (List<double>)fieldValues));
                            break;
                        case MilvusDataType.String:
                        case MilvusDataType.VarChar:
                            fieldsData.Add(FieldData.Create(fieldName, (List<string>)fieldValues));
                            break;
                        case MilvusDataType.BinaryVector:
                            fieldsData.Add(FieldData.Create(fieldName, (List<byte[]>)fieldValues));
                            break;
                        case MilvusDataType.FloatVector:
                            var convertedFieldValues =  ((List<List<float>>)fieldValues)
                                .Select(list => new ReadOnlyMemory<float>(list.ToArray()))
                                .ToList()
                                .AsReadOnly();
                            // var convertedFieldValues =  ((List<List<float>>)fieldValues)
                            //     .Select(list => list.ToArray())  // Convert each List<float> to a float[]
                            //     .ToList();
                            fieldsData.Add(FieldData.CreateFloatVector(fieldName, convertedFieldValues));
                            break;
                        default:
                            throw new Exception($"Unsupported data type: {dataType}");
                    }
                }

                return await collection.InsertAsync(fieldsData);
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }
    

    public async Task<SearchResults> SearchAsync(SearchRequest searchParams)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(searchParams.CollectionName))
            {
                var collection = _milvusClient.GetCollection(searchParams.CollectionName);
                ReadOnlyMemory<float> vectorMemory = new ReadOnlyMemory<float>(searchParams.QueryVector.ToArray());

                // Create a list of ReadOnlyMemory<float> and wrap it in a ReadOnlyCollection
                var vectorList =
                    new ReadOnlyCollection<ReadOnlyMemory<float>>(new List<ReadOnlyMemory<float>> { vectorMemory });

                // Dictionary to hold search-specific parameters
                var searchParamsDict = new Dictionary<string, string>();

                // Set metric_type
                searchParamsDict.Add("metric_type", searchParams.SimilarityMetricType.ToString());

                // Set nprobe
                if (searchParams.Nprobe.HasValue)
                {
                    searchParamsDict.Add("nprobe", searchParams.Nprobe.Value.ToString());
                }

                // Set search precision level
                if (searchParams.Level.HasValue)
                {
                    searchParamsDict.Add("level", searchParams.Level.Value.ToString());
                }

                // Set radius for search space
                if (searchParams.Radius.HasValue)
                {
                    searchParamsDict.Add("radius", searchParams.Radius.Value.ToString());
                }

                // Set range filter for the inner boundary
                if (searchParams.RangeFilter.HasValue)
                {
                    searchParamsDict.Add("range_filter", searchParams.RangeFilter.Value.ToString());
                }

                // Perform the search with the dynamic parameters
                var searchParameters = new SearchParameters
                {
                    ConsistencyLevel = searchParams.ConsistencyLevel ?? ConsistencyLevel.Strong,
                    Offset = searchParams.Offset ?? 0,
                    ExtraParameters = { ["nprobe"] = "1024" },
                    // ExtraParameters = { searchParamsDict }
                };
                foreach (var searchParamsOutputField in searchParams.OutputFields)
                {
                    searchParameters.OutputFields.Add(searchParamsOutputField);
                }

                return await collection.SearchAsync<float>(
                    searchParams.queryVectorField,
                    vectorList,
                    searchParams.SimilarityMetricType,
                    searchParams.TopK,
                    searchParameters);
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            // Consider logging the exception or providing more detailed error handling
            throw;
        }
    }


    public async Task<bool> CreateIndexAsync(CreateIndexRequest indexParams)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(indexParams.CollectionName))
            {
                var collection = _milvusClient.GetCollection(indexParams.CollectionName);
                var indexParamsDict = new Dictionary<string, string>();

                // Add parameters common to IVF-based indices
                if (indexParams.IndexType == IndexType.IvfFlat || indexParams.IndexType == IndexType.IvfSq8 ||
                    indexParams.IndexType == IndexType.IvfPq)
                {
                    indexParamsDict.Add("nlist", indexParams.Nlist.ToString());

                    if (indexParams.IndexType == IndexType.IvfPq && indexParams.M.HasValue)
                    {
                        indexParamsDict.Add("m", indexParams.M.ToString()); // Add 'm' parameter for IVF_PQ
                        if (indexParams.Nbits.HasValue)
                            indexParamsDict.Add("nbits", indexParams.Nbits.ToString()); // Optional parameter for IVF_PQ
                    }
                }

                // Add parameters for HNSW and RHNSW indices
                if (indexParams.IndexType == IndexType.Hnsw || indexParams.IndexType == IndexType.RhnswFlat ||
                    indexParams.IndexType == IndexType.RhnswSq || indexParams.IndexType == IndexType.RhnswPq)
                {
                    if (indexParams.M.HasValue)
                        indexParamsDict.Add("M", indexParams.M.ToString()); // M is required for HNSW and RHNSW
                    if (indexParams.EfConstruction.HasValue)
                        indexParamsDict.Add("efConstruction",
                            indexParams.EfConstruction.ToString()); // efConstruction parameter

                    // Add PQM for RHNSW_PQ
                    if (indexParams.IndexType == IndexType.RhnswPq && indexParams.PQM.HasValue)
                        indexParamsDict.Add("PQM", indexParams.PQM.ToString());
                }

                // Add parameters for ANNOY index
                if (indexParams.IndexType == IndexType.Annoy)
                {
                    indexParamsDict.Add("n_trees", indexParams.Nlist.ToString()); // ANNOY uses nlist as n_trees
                }

                // Create the index with the accumulated parameters
                await collection.CreateIndexAsync(indexParams.ColumnName, indexParams.IndexType,
                    indexParams.SimilarityMetricType, indexParams.IndexName,
                    indexParamsDict);
                return true;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<bool> RemoveIndexAsync(string collectionName, string columnName)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                await collection.ReleaseAsync();
                await collection.DropIndexAsync(columnName, "default");
                await collection.LoadAsync();
                return true;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<bool> LoadCollectionAsync(string collectionName)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                await collection.LoadAsync();
                return true;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<bool> ReleaseCollectionAsync(string collectionName)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                await collection.ReleaseAsync();
                return true;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<MilvusCollectionDataModel> DescribeCollectionAsync(string collectionName)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var milvusCollectionData = new MilvusCollectionDataModel();
                milvusCollectionData.CollectionName = collectionName;

                var collectionData = await collection.DescribeAsync();
                milvusCollectionData.ConsistencyLevel = collectionData.ConsistencyLevel;
                var fieldIndexData = await collection.DescribeIndexAsync(collectionData.Schema.Fields[0].Name);
                milvusCollectionData.IndexData = new CollectionFields();
                milvusCollectionData.IndexData.Fields = new List<MilvusFieldDataModel>();
                foreach (var schemaField in collectionData.Schema.Fields)
                {
                    var milvusFieldData = new MilvusFieldDataModel();
                    milvusFieldData.Id = schemaField.FieldId;
                    milvusFieldData.DataType = schemaField.DataType;
                    milvusFieldData.Name = schemaField.Name;
                    milvusFieldData.isPrimaryKey = schemaField.IsPrimaryKey;
                    var indexInfo = fieldIndexData.FirstOrDefault(x => x.FieldName == schemaField.Name);
                    if (indexInfo is not null)
                    {
                        milvusFieldData.isIndexed = true;
                        if (indexInfo.Params.TryGetValue("metric_type", out var smt) &&
                            Enum.TryParse<SimilarityMetricType>(smt, true,
                                out var similarityMetricEnum))
                        {
                            milvusFieldData.SimilarityMetricType = similarityMetricEnum;
                        }

                        if (indexInfo.Params.TryGetValue("index_type", out var it) &&
                            Enum.TryParse<IndexType>(it, true, out var IndexType))
                        {
                            milvusFieldData.IndexType = IndexType;
                        }

                        milvusCollectionData.IsCollectionIndexed = true;
                    }

                    milvusCollectionData.IndexData.Fields.Add(milvusFieldData);
                }

                if (await collection.GetLoadingProgressAsync() == 100)
                {
                    milvusCollectionData.IsCollectionLoaded = true;
                }

                var res = await collection.QueryAsync("id>0");
                if (res.Count > 0 && res[0].RowCount > 0)
                {
                    milvusCollectionData.DoCollectionHaveData = true;
                }

                return milvusCollectionData;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }
    
    public async Task<long> GetTotalData(string collectionName)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var data = await collection.GetEntityCountAsync();
                return data;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }
    
    public async Task<long> DeleteDataByProperties(string collectionName, string expression)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var data = await collection.DeleteAsync(expression);
                return data.DeleteCount;
            }
            else
            {
                throw new Exception("Cannot Delete Data!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    } 
    
    public async Task<long> DeleteDataByPrimaryId(string collectionName, string expression)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var data = await collection.DeleteAsync(expression);
                return data.DeleteCount;
            }
            else
            {
                throw new Exception("Cannot Delete Data!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    } 
    
    public async Task<long> DeleteAllData(string collectionName, string expression)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var data = await collection.DeleteAsync(expression);
                return data.DeleteCount;
            }
            else
            {
                throw new Exception("Cannot Delete Data!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<MutationResult> UpdateCollectionAsync(string collectionName, List<MilvusDataModel> milvusData)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(collectionName))
            {
                var collection = _milvusClient.GetCollection(collectionName);
                var ids = milvusData.Select(x => x.Id).ToList();
                var vectors = milvusData.Select(x => x.Embedding).ToList();
                var guidIdsList = milvusData.Select(x => x.GuidId).ToList();
                var fieldData = new List<FieldData>();
                fieldData.Add(FieldData.Create("id", ids));
                fieldData.Add(FieldData.Create("vector", vectors));
                fieldData.Add(FieldData.Create("GuidId", guidIdsList));

                var result = await collection.UpsertAsync(fieldData);
                Console.WriteLine(result.UpsertCount);
                if (result.ErrorIndex.Count > 0)
                {
                    Console.WriteLine("Error in ids : ");
                    foreach (var id in result.ErrorIndex)
                    {
                        Console.WriteLine(id);
                    }
                }

                return result;
            }
            else
            {
                throw new Exception("Collection not created!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    public async Task<List<string>> GetCollectionsAsync()
    {
        try
        {
            var collectionNames = new List<string>();
            if (_milvusClient != null)
            {
                var collections = await _milvusClient.ListCollectionsAsync();
                collectionNames = collections.Select(x => x.Name).ToList();
                return collectionNames;
            }
            else
            {
                throw new Exception("milvus not connected!!!");
            }
        }
        catch (Exception exception)
        {
            throw;
        }
    }
}