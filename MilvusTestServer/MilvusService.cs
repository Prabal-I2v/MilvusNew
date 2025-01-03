using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text.Json;
using Milvus.Client;
using MilvusTestServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Enum = System.Enum;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

public class MilvusService
{
    private MilvusClient _milvusClient;
    private readonly HttpClient _httpClient;
    protected TimeGenerator timeGenerator;

    private static List<Tuple<string, string, string, string, List<float>>> faceEmebeddingData =
        new List<Tuple<string, string, string, string, List<float>>>();

    private Random random;
    public static int insertionLock = 0;

    public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public static void Init()
    {
        LoadFaceEmbeddingData("D:\\i2v\\i2vProjects\\EventGenerator_AnalyticManager\\cropped_files\\_60000.csv");
    }

    public MilvusService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        random = new Random();
        timeGenerator = CreateTimeGenerator("random");
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
                                    var res2 = fieldData.Value;
                                    if (fieldData.Value is JsonElement jsonElement)
                                    {
                                        var strData = jsonElement.ToString();
                                        res2 = JsonConvert.DeserializeObject<List<float>>(strData);
                                    }

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
                            var convertedFieldValues = ((List<List<float>>)fieldValues)
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

                var startTime = DateTime.UtcNow; // Record the start time

                var searchParameters = new SearchParameters
                {
                    ConsistencyLevel = ConsistencyLevel.Strong,
                    Offset = 0,
                    ExtraParameters = { ["nprobe"] = "1024" },
                    // ExtraParameters = { searchParamsDict }
                };
                var searchParams = new MilvusVectorSearchModel<float>()
                {
                    CancellationToken = new CancellationToken(),
                    Limit = 1,
                    MetricType = SimilarityMetricType.L2,
                    Parameters = searchParameters,
                   
                    VectorFieldName = "embedding"
                };
                searchParams.Vectors = milvusData
                    .Select(x => x.Fields
                        .Where(y => y.Name == "embedding")
                        .Select(z => new ReadOnlyMemory<float>(z.Value.ToArray())) // Convert List<float> to array and then to ReadOnlyMemory<float>
                        .FirstOrDefault())  // Assuming you want the first "embedding" field
                    .Where(v => v.Length > 0)  // Ensure non-empty results
                    .ToList();  // Convert to List<ReadOnlyMemory<float>>

                var searchData = await SearchInMilvus(collectionName, searchParams);
        
                // var result = await collection.InsertAsync(fieldsData); // Perform the async operation

                var endTime = DateTime.UtcNow; // Record the end time

                // Calculate the time difference
                var timeTaken = endTime - startTime;
                Console.WriteLine($"Time taken: {timeTaken.TotalMilliseconds} ms");
                return null;
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

    protected Dictionary<string, dynamic> Generate(string eventType)
    {
        // select a random entry from faceEmebeddingData
        var faceData = faceEmebeddingData[random.Next(0, faceEmebeddingData.Count)];

        var t = timeGenerator.NextAsUnixMilliseconds();
        var _event = new Dictionary<string, dynamic>();
        if (eventType == "watchList")
        {
            _event.Add("embedding", faceData.Item5);
            _event.Add("faceId", Guid.NewGuid());
            _event.Add("personId", Guid.NewGuid());
        }
        else
        {
            _event.Add("embedding", faceData.Item5);
            _event.Add("track_Id", Guid.NewGuid());
            _event.Add("group_Id", Guid.NewGuid());
            _event.Add("device_Id", Guid.NewGuid());
            _event.Add("event_time", t);
        }

        return _event;
    }

    private static void LoadFaceEmbeddingData(string csvfilepath)
    {
        var lines = System.IO.File.ReadAllLines(csvfilepath);
        // first line is header
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var parts = line.Split(';');
            var Name = parts[0];
            var Label = parts[1];
            var Gender = parts[2];
            // the next part is list containing [{'Embedding': [0.03565146028995514, ...], 'Image': '/9j/...' }]
            // we need to add a new entry in faceEmebeddingData for each of these
            var json = parts[3];
            var obj = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            foreach (var item in obj)
            {
                JArray emb = item["Embedding"] as JArray;
                var embedding = emb.ToObject<List<float>>();
                var image = item["Image"] as string;
                faceEmebeddingData.Add(
                    new Tuple<string, string, string, string, List<float>>(Name, Label, Gender, image, embedding));
            }
        }
    }


    public async Task<SearchResults> SearchByParams(SearchRequest searchParams)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(searchParams.CollectionName))
            {
                var collection = _milvusClient.GetCollection(searchParams.CollectionName);
                ReadOnlyMemory<float> vectorMemory =
                    new ReadOnlyMemory<float>(searchParams.vectorSearchRequest.QueryVector.ToArray());

                // Create a list of ReadOnlyMemory<float> and wrap it in a ReadOnlyCollection
                var vectorList =
                    new ReadOnlyCollection<ReadOnlyMemory<float>>(new List<ReadOnlyMemory<float>> { vectorMemory });

                // Dictionary to hold search-specific parameters
                var searchParamsDict = new Dictionary<string, string>();

                // Set metric_type
                searchParamsDict.Add("metric_type", searchParams.vectorSearchRequest.SimilarityMetricType.ToString());

                // Set nprobe
                if (searchParams.vectorSearchRequest.Nprobe.HasValue)
                {
                    searchParamsDict.Add("nprobe", searchParams.vectorSearchRequest.Nprobe.Value.ToString());
                }

                // Set search precision level
                if (searchParams.vectorSearchRequest.Level.HasValue)
                {
                    searchParamsDict.Add("level", searchParams.vectorSearchRequest.Level.Value.ToString());
                }

                // Set radius for search space
                if (searchParams.vectorSearchRequest.Radius.HasValue)
                {
                    searchParamsDict.Add("radius", searchParams.vectorSearchRequest.Radius.Value.ToString());
                }

                // Set range filter for the inner boundary
                if (searchParams.vectorSearchRequest.RangeFilter.HasValue)
                {
                    searchParamsDict.Add("range_filter", searchParams.vectorSearchRequest.RangeFilter.Value.ToString());
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
        foreach (var f in vectorList[0].ToArray())
        {
            Console.WriteLine(f.ToString() + ',');
        }
                var res = await SearchInMilvus(collectionName: searchParams.CollectionName, new MilvusVectorSearchModel<float>()
                {
                    CancellationToken = new CancellationToken(),
                    Limit = searchParams.TopK,
                    MetricType = searchParams.vectorSearchRequest.SimilarityMetricType,
                    Parameters = searchParameters,
                    Vectors = vectorList,
                    VectorFieldName = searchParams.vectorSearchRequest.queryVectorField
                });

                return res;
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

    public async Task<SearchResults> SearchInMilvus<T>(string collectionName, MilvusVectorSearchModel<T> searchParams)
    {
        var collection = _milvusClient.GetCollection(collectionName);
        return await collection.SearchAsync<T>(
            searchParams.VectorFieldName,
            searchParams.Vectors,
            searchParams.MetricType,
            searchParams.Limit,
            searchParams.Parameters);
    }

    public async Task<IReadOnlyList<FieldData>> SearchData(SearchRequest searchRequest)
    {
        try
        {
            if (await _milvusClient.HasCollectionAsync(searchRequest.CollectionName))
            {
                var collection = _milvusClient.GetCollection(searchRequest.CollectionName);
                var expression =
                    GenericFilter<SearchRequest>.BuildExpression(searchRequest.PrimaryKey, "",
                        ComparisonOperator.NotEqual);
                var queryParams = new QueryParameters();
                queryParams.ConsistencyLevel = searchRequest.ConsistencyLevel;
                queryParams.Limit = searchRequest.TopK;
                foreach (var fields in searchRequest.OutputFields)
                {
                    queryParams.OutputFields.Add(fields);
                }

                var searchResults = await collection.QueryAsync(expression, queryParams);
                return searchResults;
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
                await collection.FlushAsync();
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

    public async Task UploadData(string collectionName, int noOfEvents)
    {
        MutationResult res = null;
        if (Interlocked.CompareExchange(ref insertionLock, 1, 0) == 0)
        {
            try
            {
                if (await _milvusClient.HasCollectionAsync(collectionName))
                {
                    if (noOfEvents == -1)
                    {
                        while (!cancellationTokenSource.IsCancellationRequested)
                        {
                            var data = MakeInsertDataRequest("watchList", 1000);
                            res = await InsertDataAsync(collectionName, data);
                        }
                    }
                    else
                    {
                        var data = MakeInsertDataRequest("watchList", noOfEvents);
                        res = await InsertDataAsync(collectionName, data);
                    }
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
            finally
            {
                Interlocked.Exchange(ref insertionLock, 0);
            }
        }
    }

    public async Task StopUploadData()
    {
        try
        {
            cancellationTokenSource.Cancel();
        }
        catch (Exception exception)
        {
            throw;
        }
    }

    private List<InsertDataRequest> MakeInsertDataRequest(string eventType, int noOfEvents = 1000)
    {
        List<InsertDataRequest> insertDataList = new List<InsertDataRequest>();
        while (noOfEvents-- > 0)
        {
            var _event = Generate(eventType);
            // Parse each line as a JSON object
            try
            {
                var jsonData = _event;
                if (jsonData != null)
                {
                    var insertDataRequest = ConvertToInsertDataRequest(jsonData);
                    insertDataList.Add(insertDataRequest);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse line as JSON: {ex.Message}");
            }
        }

        return insertDataList;
    }

    // Helper method to convert raw JSON data into InsertDataRequest
    public InsertDataRequest ConvertToInsertDataRequest(Dictionary<string, dynamic> jsonData)
    {
        var request = new InsertDataRequest
        {
            Fields = new List<InsertDataFieldData>(),
            AdditionalData = new Dictionary<string, string>()
        };

        // Assuming 'embedding', 'faceId', and 'personId' are fields you want to map to InsertDataFieldData
        foreach (var entry in jsonData)
        {
            if (entry.Key == "embedding")
            {
                request.Fields.Add(new InsertDataFieldData
                {
                    Name = "embedding",
                    Value = entry.Value,
                    DataType = MilvusDataType.FloatVector
                });
            }
            else if (entry.Key == "faceId" || entry.Key == "personId")
            {
                request.Fields.Add(new InsertDataFieldData
                {
                    Name = entry.Key,
                    Value = entry.Value,
                    DataType = MilvusDataType.String
                });
            }
        }

        return request;
    }

    public interface TimeGenerator
    {
        public DateTimeOffset Next();
        public long NextAsUnixMilliseconds();
    }

    private static TimeGenerator CreateTimeGenerator(string timeMode)
    {
        TimeGenerator timeGenerator;
        switch (timeMode)
        {
            case "incremental":
                timeGenerator = new RandomDateTime(DateTime.Now);
                break;
            case "random":
                timeGenerator = new IncrementalDateTime(DateTime.Now.Subtract(TimeSpan.FromDays(160)));
                break;
            default:
                throw new ArgumentException("Invalid time mode");
        }

        return timeGenerator;
    }

    public class RandomDateTime : TimeGenerator
    {
        DateTime start;
        Random gen;
        int range;

        public RandomDateTime(DateTime start, DateTime end = default(DateTime))
        {
            gen = new Random();
            this.start = start;
            // if end is not provided, use today's date
            var upto = end == default(DateTime) ? DateTime.Now : end;
            range = (upto - start).Days;
        }

        public DateTimeOffset Next()
        {
            var d = DateTime.SpecifyKind(
                start.AddDays(gen.Next(range)).AddHours(gen.Next(0, 24)).AddMinutes(gen.Next(0, 60))
                    .AddSeconds(gen.Next(0, 60)), DateTimeKind.Utc);
            DateTimeOffset utcTime = d;
            return utcTime;
        }

        public long NextAsUnixMilliseconds()
        {
            var d = Next();
            return d.ToUnixTimeMilliseconds();
        }
    }

    public class IncrementalDateTime : TimeGenerator
    {
        DateTime start;
        int interval;
        bool hasRandomness;

        public IncrementalDateTime(DateTime start, int interval = 1, bool hasRandomness = false)
        {
            this.start = start;
            this.interval = interval;
            this.hasRandomness = hasRandomness;
        }

        public DateTimeOffset Next()
        {
            this.start = start.AddSeconds(interval);
            if (hasRandomness)
            {
                var random = new Random();
                this.start = this.start.AddSeconds(random.Next(0, 60));
            }

            return this.start;
        }

        public long NextAsUnixMilliseconds()
        {
            var d = Next();
            return d.ToUnixTimeMilliseconds();
        }
    }
}