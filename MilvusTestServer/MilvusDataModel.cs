using Milvus.Client;

namespace MilvusTestServer;

public class MilvusDataModel
{
    public long Id { get; set; }
    public List<float> Embedding { get; set; } = new List<float>();
    public Guid GuidId { get; set; } = Guid.NewGuid();
}

public class MilvusCollectionDataModel
{
    public string CollectionName { get; set; }
    public CollectionFields IndexData { get; set; }
    public bool IsCollectionLoaded { get; set; } = false;
    public bool IsCollectionIndexed { get; set; } = false;
    public bool DoCollectionHaveData { get; set; } = false;
    public ConsistencyLevel ConsistencyLevel { get; set; }
}

public class CollectionFields
{
    public List<MilvusFieldDataModel> Fields { get; set; }
}

public class MilvusFieldDataModel
{
    public dynamic Id { get; set; }
    public string Name { get; set; }
    public MilvusDataType DataType { get; set; }
    public bool isIndexed { get; set; } = false;
    public bool isPrimaryKey { get; set; } = false;
    public SimilarityMetricType? SimilarityMetricType { get; set; } = Milvus.Client.SimilarityMetricType.Invalid;
    public IndexType? IndexType { get; set; } = Milvus.Client.IndexType.Invalid;
}

public class CreateIndexRequest
{
    public string CollectionName { get; set; }
    public string ColumnName { get; set; }
    public string IndexName { get; set; }
    public IndexType IndexType { get; set; }
    public SimilarityMetricType SimilarityMetricType { get; set; }
    public int Nlist { get; set; }
    public int? Nbits { get; set; } // Optional for IVF_PQ
    public int? M { get; set; } // Optional for HNSW and RHNSW
    public int? EfConstruction { get; set; } // Optional for HNSW and RHNSW
    public int? PQM { get; set; } // Optional for RHNSW_PQ
}

public class VectorSearchRequest
{
    /// <summary>
    /// The vector to use for the search query.
    /// </summary>
    public List<float> QueryVector { get; set; }

    public string queryVectorField { get; set; }

    /// <summary>
    /// Metric type used to measure similarity. Possible values: IP, L2, COSINE, JACCARD, HAMMING.
    /// </summary>
    public SimilarityMetricType SimilarityMetricType { get; set; }

    /// <summary>
    /// Number of units to query during the search (optional).
    /// </summary>
    public int? Nprobe { get; set; }

    /// <summary>
    /// Search precision level (optional). Possible values: 1, 2, 3.
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// Outer boundary of the search space (optional).
    /// </summary>
    public float? Radius { get; set; }

    /// <summary>
    /// Inner boundary of the search space (optional).
    /// </summary>
    public float? RangeFilter { get; set; }
}

public class MilvusVectorSearchModel<T>
{
    // Fields
    public string VectorFieldName { get; set; }
    public IReadOnlyList<ReadOnlyMemory<T>> Vectors { get; set; }
    public SimilarityMetricType MetricType { get; set; }
    public int Limit { get; set; }
    public SearchParameters? Parameters { get; set; }
    public CancellationToken? CancellationToken { get; set; } = default(CancellationToken);

    // Constructor to initialize the model
}
public class SearchRequest
{
    /// <summary>
    /// Name of the collection to search in.
    /// </summary>
    public string CollectionName { get; set; }

    /// <summary>
    /// Number of top results to return.
    /// </summary>
    public int TopK { get; set; }

    public string PrimaryKey { get; set; }

    public MilvusDataType PrimaryKeyType { get; set; }

    /// <summary>
    /// Optional offset for pagination.
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// Consistency level for the search operation (optional).
    /// </summary>
    public ConsistencyLevel? ConsistencyLevel { get; set; }

    /// <summary>
    /// List of output fields to return from the search results.
    /// </summary>
    public List<string> OutputFields { get; set; }

    public VectorSearchRequest? vectorSearchRequest { get; set; }

    public SearchRequest()
    {
        OutputFields = new List<string>();
    }
}

public class FieldsDetails
{
    public string Name { get; set; }
    public MilvusDataType Type { get; set; }
}

public class InsertDataRequest
{
    public List<InsertDataFieldData> Fields { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; }
}

public class InsertDataFieldData
{
    public string Name { get; set; }
    public dynamic Value { get; set; }
    public MilvusDataType DataType { get; set; }
}