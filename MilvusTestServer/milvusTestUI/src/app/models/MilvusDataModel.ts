// Model representing Milvus Collection Data
export interface MilvusCollectionDataModel {
  collectionName: string;
  indexData: CollectionFields;
  doCollectionHaveData: boolean;
  isCollectionLoaded: boolean;
  isCollectionIndexed: boolean;
  consistencyLevel: ConsistencyLevel;
}

// Model representing Milvus Index Data
export interface CollectionFields {
  fields: MilvusFieldDataModel[];
}

// Model representing Milvus Field Data
export interface MilvusFieldDataModel {
  id: any; // Using 'any' to represent dynamic types, adjust based on your use case
  name: string;
  dataType: number;
  isIndexed: boolean;
  similarityMetricType: SimilarityMetricType;
  indexType: IndexType;
  isPrimaryKey: boolean;
}

export enum MilvusDataType {
  None = 0,          // None
  Bool = 1,          // Boolean type
  Int8 = 2,          // 8-bit integer
  Int16 = 3,         // 16-bit integer
  Int32 = 4,         // 32-bit integer
  Int64 = 5,         // 64-bit integer
  Float = 10,        // 32-bit floating point
  Double = 11,       // 64-bit floating point
  String = 20,       // String type
  VarChar = 21,      // Variable-length string
  Array = 22,        // Array type
  Json = 23,         // JSON type
  BinaryVector = 100, // Binary vector
  FloatVector = 101   // Floating point vector
}

export const MilvusDataTypeMapping: { [key: number]: string } = {
  1: "bool",
  5: "number",
  6: "number",
  7: "number",
  8: "string",
  9: "string",
  101: "floatArray",
}

// index-creation-params.model.ts
export interface IndexCreationParams {
  collectionName: string;
  columnName: string;
  indexName: string;
  indexType: IndexType;  // Enum for index type
  similarityMetricType: SimilarityMetricType;  // Enum for similarity metric type
  nlist: number;
  nbits?: number;
  m?: number;
  M?: number;
  efConstruction?: number;
  PQM?: number;
}

export enum IndexType {
  Invalid = 0,

  // Flat index: provides perfect accuracy and exact search results, but is slow.
  Flat,

  // IvfFlat index: clusters vectors and reduces query time by searching in the most similar clusters.
  IvfFlat,

  // IvfSq8 index: compresses vectors using scalar quantization, reducing memory consumption by 70-75%.
  IvfSq8,

  // IvfPq index: uses product quantization, reducing search time and space complexity but with loss of accuracy.
  IvfPq,

  // Hnsw index: builds a graph-based multi-layer navigation structure to approach target positions quickly.
  Hnsw,

  // DiskANN: similar to IvfPq but uses SIMD for efficient calculations.
  DiskANN,

  // Annoy index: divides high-dimensional space into subspaces and stores them in a tree structure.
  Annoy,

  // RHNSW indices: variations of Hnsw with different quantization methods (Flat, Pq, Sq).
  RhnswFlat,
  RhnswPq,
  RhnswSq,

  // Binary indices: for binary data.
  BinFlat,
  BinIvfFlat,

  // AutoIndex: automatically selects the best index based on the data.
  AutoIndex
}

export enum SimilarityMetricType {
  Invalid,

  // Euclidean distance (L2): measures the length of a segment connecting two points, valid for float vectors.
  L2,

  // Inner product (IP): measures the orientation, not the magnitude, of vectors, valid for float vectors.
  Ip,

  // Cosine similarity: measures the cosine of the angle between two non-zero vectors in multidimensional space, valid for float vectors.
  Cosine,

  // Jaccard similarity: measures similarity between two sets based on their intersection and union, valid for binary vectors.
  Jaccard,

  // Tanimoto distance: measures similarity for binary variables, valid for binary vectors.
  Tanimoto,

  // Hamming distance: measures the number of differing bit positions between two binary strings, valid for binary vectors.
  Hamming,

  // Superstructure: measures similarity between a chemical structure and its superstructure, valid for binary vectors.
  Superstructure,

  // Substructure: measures similarity between a chemical structure and its substructure, valid for binary vectors.
  Substructure
}

export class VectorSearchRequest {
  /**
   * The vector to use for the search query.
   */
  queryVector: number[];

  /**
   * The field representing the query vector.
   */
  queryVectorField: string;

  /**
   * Metric type used to measure similarity.
   * Possible values: IP, L2, COSINE, JACCARD, HAMMING.
   */
  similarityMetricType: SimilarityMetricType;

  /**
   * Number of units to query during the search (optional).
   */
  nprobe?: number;

  /**
   * Search precision level (optional). Possible values: 1, 2, 3.
   */
  level?: number;

  /**
   * Outer boundary of the search space (optional).
   */
  radius?: number;

  /**
   * Inner boundary of the search space (optional).
   */
  rangeFilter?: number;

  constructor(
    queryVector: number[],
    queryVectorField: string,
    similarityMetricType: SimilarityMetricType,
    nprobe?: number,
    level?: number,
    radius?: number,
    rangeFilter?: number
  ) {
    this.queryVector = queryVector;
    this.queryVectorField = queryVectorField;
    this.similarityMetricType = similarityMetricType;
    this.nprobe = nprobe;
    this.level = level;
    this.radius = radius;
    this.rangeFilter = rangeFilter;
  }
}

export class SearchRequest {
  /**
   * Name of the collection to search in.
   */
  collectionName: string;

  /**
   * Number of top results to return.
   */
  topK: number;

  /**
   * Optional offset for pagination.
   */
  offset?: number;

  /**
   * Consistency level for the search operation (optional).
   */
  consistencyLevel?: ConsistencyLevel;

  /**
   * List of output fields to return from the search results.
   */
  outputFields: string[];

  primaryKey: string;
  primaryKeyType : MilvusDataType;

  /**
   * Nested vector search request containing vector-related parameters.
   */
  vectorSearchRequest?: VectorSearchRequest;

  constructor(
    collectionName: string,
    topK: number,
    outputFields: string[],
    primaryKey: string,
    primaryKeyType: MilvusDataType,
    vectorSearchRequest?: VectorSearchRequest,
    offset?: number,
    consistencyLevel?: ConsistencyLevel
  ) {
    this.collectionName = collectionName;
    this.topK = topK;
    this.outputFields = outputFields;
    this.primaryKey = primaryKey;
    this.primaryKeyType = primaryKeyType;
    this.vectorSearchRequest = vectorSearchRequest;
    this.offset = offset;
    this.consistencyLevel = consistencyLevel;
  }
}

export enum ConsistencyLevel {
  /**
   * The highest and the most strict level of consistency. This level ensures that users can read the latest
   * version of data.
   */
  Strong = 0,

  /**
   * Ensures that all data writes can be immediately perceived in reads during the same session. In other
   * words, when you write data via one client, the newly inserted data instantaneously become searchable.
   */
  Session = 1,

  /**
   * Allows data inconsistency during a certain period of time. However, generally, the data are always globally
   * consistent out of that period of time.
   */
  BoundedStaleness = 2,

  /**
   * There is no guaranteed order of reads and writes, and replicas eventually converge to the same state given that
   * no further write operations are done. Under this level, replicas start working on read requests with the latest
   * updated values. Eventually consistent is the weakest of the four consistency levels.
   */
  Eventually = 3,

  /**
   * In this consistency level, users pass their own guarantee timestamp.
   */
  Customized = 4
}

export interface InsertDataRequest {
  fields: InsertDataFieldData[];
  additionalData: { [key: string]: string }
}

export interface InsertDataFieldData {
  name: string;
  value: any; // Use 'any' for dynamic typing, you can narrow it down if needed
  dataType: MilvusDataType;
}








