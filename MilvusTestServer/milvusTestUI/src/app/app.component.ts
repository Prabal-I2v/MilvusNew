// app.component.ts
import {ChangeDetectorRef, Component, OnChanges, OnInit} from '@angular/core';
import {MilvusService} from './milvus.service';
import {
  ConsistencyLevel,
  IndexCreationParams,
  IndexType,
  MilvusCollectionDataModel,
  MilvusDataTypeMapping, MilvusFieldDataModel, SearchRequest,
  SimilarityMetricType
} from "./models/MilvusDataModel";
import {TimelineStage} from "./timeline/timeline.component";
import {AbstractControl, FormArray, FormBuilder, FormGroup, Validators} from "@angular/forms";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit, OnChanges {
  collectionName = '';
  dimension = 512;
  vectors = [[0.1, 0.2, 0.3], [0.4, 0.5, 0.6]];
  queryVector = [0.1, 0.2, 0.3];
  topK = 2;
  collections: string[] = [];
  selectedCollectionName: string = '';
  collectionStatus: string = ''; // Status updated based on operations
  selectedCollectionData: MilvusCollectionDataModel = {} as MilvusCollectionDataModel;
  arrayField: string = '';
  guidId: string = '';
  form: FormGroup;
  ipAddress = 'localhost'; // default value
  port = 19530; // default port
  isConnected = false;
  connectionError = false;
  isCreatingIndex: boolean = false;
  isRemovingIndex: boolean = false;
  isLoadingCollection: boolean = false;
  isReleasingCollection: boolean = false;
  isInsertingData: boolean = false;
  columnForRemoveIndexing: string = '';
  IndexTypes = IndexType;

  collectionStages: TimelineStage = {
    IsIndexed: false,
    IsLoaded: false,
    HasData: false,
  }

  indexTypes = Object.keys(IndexType).filter(key => isNaN(Number(key)));  // Use enum values
  similarityMetrics = Object.keys(SimilarityMetricType).filter(key => isNaN(Number(key)));  // Use enum values
  consistencyLevel = Object.keys(ConsistencyLevel).filter(key => isNaN(Number(key)));  // Use enum values

  indexCreationParams: IndexCreationParams = {
    collectionName: '',
    columnName: '',
    indexName: 'default',
    indexType: IndexType.IvfFlat,  // Default value
    similarityMetricType: SimilarityMetricType.Cosine,  // Default value
    nlist: 1024,
  };

  searchRequestParams: SearchRequest = {
    collectionName: '',
    queryVector: [],  // Empty array as default
    topK: 10,  // Set a default value, for example 10
    similarityMetricType: SimilarityMetricType.Cosine,  // Default value
    outputFields: ['ids'],  // Default to return 'ids'
    queryVectorField: '',

    // Optional fields can remain undefined if no default is needed
    nprobe: undefined,
    level: undefined,
    radius: undefined,
    rangeFilter: undefined,
    offset: undefined,
    consistencyLevel: undefined
  };

  columns: string[] = [];
  indexedColumns: string[] = [];

  // Mapping objects
  private similarityMetricTypeMap: { [key: string]: SimilarityMetricType } = {
    'L2': SimilarityMetricType.L2,
    'Ip': SimilarityMetricType.Ip,
    'Cosine': SimilarityMetricType.Cosine,
    'Jaccard': SimilarityMetricType.Jaccard,
    'Tanimoto': SimilarityMetricType.Tanimoto,
    'Hamming': SimilarityMetricType.Hamming,
    'Superstructure': SimilarityMetricType.Superstructure,
    'Substructure': SimilarityMetricType.Substructure
  };

  private consistencyLevelMap: { [key: string]: ConsistencyLevel } = {
    'Strong' : ConsistencyLevel.Strong,
    'Session' : ConsistencyLevel.Session,
    'BoundedStaleness' : ConsistencyLevel.BoundedStaleness,
    'Eventually' : ConsistencyLevel.Eventually,
    'Customized' : ConsistencyLevel.Customized
  };


  private indexTypeMap: { [key: string]: IndexType } = {
    'Invalid': IndexType.Invalid,
    'Flat': IndexType.Flat,
    'IvfFlat': IndexType.IvfFlat,
    'IvfSq8': IndexType.IvfSq8,
    'IvfPq': IndexType.IvfPq,
    'Hnsw': IndexType.Hnsw,
    'DiskANN': IndexType.DiskANN,
    'Annoy': IndexType.Annoy,
    'RhnswFlat': IndexType.RhnswFlat,
    'RhnswPq': IndexType.RhnswPq,
    'RhnswSq': IndexType.RhnswSq,
    'BinFlat': IndexType.BinFlat,
    'BinIvfFlat': IndexType.BinIvfFlat,
    'AutoIndex': IndexType.AutoIndex
  };

  searchForm: FormGroup;

  allOutputFields: string[] = [];
  selectedOutputFields: string[] = [];


  constructor(public milvusService: MilvusService, public fb: FormBuilder, public cdrRef: ChangeDetectorRef) {
    this.form = this.fb.group({
      fields: this.fb.array([]),
    });

    this.searchForm = this.fb.group({
      collectionName: ['', Validators.required], // Required field
      queryVector: ["", Validators.required], // Required field
      topK: [10, [Validators.required, Validators.min(1)]], // Default to 10, required, and minimum value of 1
      similarityMetricType: ["", Validators.required], // Default to cosine, required
      nprobe: [null], // Optional
      level: [null], // Optional
      radius: [null], // Optional
      rangeFilter: [null], // Optional
      offset: [null], // Optional
      consistencyLevel: [ConsistencyLevel.Strong], // Default to Strong, optional
      outputFields: [[], Validators.required], // Required field
      queryVectorField: ['', Validators.required], // Required field
      fieldsArray: this.fb.array([]), // Initialize empty FormArray for dynamic fields
    });
  }

  ngOnInit() {
    this.fetchCollections();
    this.form.valueChanges.subscribe((data) => {
      console.log('Form Value Changes:', data);
    });

  }

  ngOnChanges() {
    // if (this.collections.length > 0 && !this.selectedCollectionName) {
    //   this.selectedCollectionName = this.collections[0]
    // }
    this.describeCollection(this.selectedCollectionName);
    this.initializeFields();
  }

  get fieldsArray(): FormArray {
    return this.form.get('fields') as FormArray;
  }

  setMetricType(event: Event) {
    const target = event.target as HTMLSelectElement;
    var val = target.value;
    this.indexCreationParams.similarityMetricType = this.getSimilarityMetric(val);
  }

  setIndexType(event: Event) {
    const target = event.target as HTMLSelectElement;
    var val = target.value;
    this.indexCreationParams.indexType = this.getIndexType(val);
  }

  // Helper method to convert string to enum for SimilarityMetric
  private getSimilarityMetric(val: string): SimilarityMetricType {
    const metric = this.similarityMetricTypeMap[val];
    return metric;
  }

  // Helper method to convert string to enum for IndexType
  private getIndexType(val: string): IndexType {
    const index = this.indexTypeMap[val];
    return index;
  }

  onCollectionSelected(collectionName: string) {
    this.indexCreationParams.collectionName = collectionName; // Auto-fill collection name
    this.indexCreationParams.columnName = ''; // Reset columnName when collection changes
    this.columns = this.selectedCollectionData.indexData?.fields.map(field => field.name) || [];
    if (this.columns.length > 0) {
      this.indexCreationParams.columnName = this.columns[0]; // Set the first column as default
    }

    this.indexedColumns = this.selectedCollectionData.indexData?.fields
      .filter(field => field.isIndexed)
      .map(field => field.name) || [];

    this.columnForRemoveIndexing = this.indexedColumns[0];
    this.cdrRef.detectChanges(); // Ensure UI updates
  }

  connectToMilvus() {
    this.milvusService.connectToMilvus(this.ipAddress, this.port).subscribe(
      (response) => {
        if (response.success) {
          this.isConnected = true;
          this.connectionError = false;
        } else {
          this.connectionError = true;
        }
      },
      (error) => {
        this.connectionError = true;
        console.error('Failed to connect to Milvus:', error);
      }
    );
  }

  initializeFields(): void {
    this.fieldsArray.clear(); // Clear existing controls
    this.allOutputFields = [];
    if (this.selectedCollectionData.indexData && this.selectedCollectionData.indexData.fields) {
      //initialize search form
      this.searchForm.get('collectionName')?.patchValue(this.selectedCollectionName);
      this.searchForm.get('consistencyLevel')?.patchValue(this.selectedCollectionData.consistencyLevel);

      this.selectedCollectionData.indexData.fields.forEach((field: MilvusFieldDataModel) => {
        this.allOutputFields.push(field.name);

        //initialize insert data form
        this.fieldsArray.push(
          this.fb.group({
            name: [field.name],
            value: ['', Validators.required], // Add validators as needed
          })
        );


        //initialize search form
        if (this.MilvusDataTypeMapping[field.dataType] === 'floatArray') {
          this.searchForm.get('queryVectorField')?.patchValue(field.name);
          this.searchForm.get('similarityMetricType')?.patchValue(this.similarityMetrics[field.similarityMetricType]);

        }

      });
    }
    this.cdrRef.detectChanges(); // Trigger change detection
  }

  fetchCollections() {
    this.milvusService.getCollections().subscribe(
      (data) => {
        this.collections = data;
        // Reset selectedCollection if no collections available
        if (this.collections.length === 0) {
          this.selectedCollectionName = '';
        }
      },
      (error) => {
        console.error('Failed to fetch collections:', error);
      }
    );
  }

  createCollection() {
    if (this.collectionName && this.dimension > 0) {
      this.milvusService.createCollection(this.collectionName, this.dimension).subscribe(() => {
        this.collectionStatus = 'Created';
        this.collectionName = ''; // Reset fields after creation
        this.dimension = 128; // Reset to default dimension
        this.fetchCollections(); // Refresh the collection list
      });
    }
  }

  createIndex() {
    this.isCreatingIndex = true;
    this.milvusService.createIndex(this.indexCreationParams).subscribe(() => {
      this.isCreatingIndex = false;
      // Handle success
      this.refreshCollectionStatus();
    }, (error) => {
      this.isCreatingIndex = false; // Stop loader on error
      console.error(error);
    });
  }

  removeIndex() {
    this.isRemovingIndex = true;
    const columnName = this.columnForRemoveIndexing;
    this.milvusService.removeIndex(this.selectedCollectionName, columnName).subscribe(() => {
      this.isRemovingIndex = false;
      console.log(`Index removed from column ${columnName}`);
      this.refreshCollectionStatus();
    }, (error) => {
      this.isRemovingIndex = false;
      console.error(`Failed to remove index from column ${columnName}:`, error);
    });
  }

  loadCollection() {
    this.isLoadingCollection = true;
    this.milvusService.loadCollection(this.selectedCollectionName).subscribe(() => {
      this.collectionStages.IsLoaded = true;
      this.isLoadingCollection = false;
      this.refreshCollectionStatus();
    }, (error) => {
      this.isLoadingCollection = false; // Stop loader on error
    });
  }

  releaseCollection() {
    this.isReleasingCollection = true;
    this.milvusService.releaseCollection(this.selectedCollectionName).subscribe(() => {
      this.collectionStages.IsLoaded = false;
      this.isReleasingCollection = false;
      this.refreshCollectionStatus();
    }, (error) => {
      this.isReleasingCollection = false; // Stop loader on error
    });
  }

  search() {
      // Prepare the SearchRequest object
      if (this.searchForm.valid) {
        const {
          outputFields,
          queryVector,
          topK,
          similarityMetricType,
          nprobe,
          level,
          radius,
          rangeFilter,
          offset,
          consistencyLevel,
          queryVectorField
        } = this.searchForm.value;

        // Prepare the SearchRequest object
        this.searchRequestParams = {
          collectionName: this.selectedCollectionData.collectionName, // Use the selected collection name
          queryVector: this.parseQueryVector(queryVector), // Parse the query vector string into an array
          topK: topK,
          similarityMetricType: this.getSimilarityMetric(similarityMetricType),
          outputFields: outputFields, // Add the selected search fields
          queryVectorField: queryVectorField, // Fill the query vector field

          // Optional fields
          nprobe: nprobe || undefined, // If null, set to undefined
          level: level || undefined, // If null, set to undefined
          radius: radius || undefined, // If null, set to undefined
          rangeFilter: rangeFilter || undefined, // If null, set to undefined
          offset: offset || undefined, // If null, set to undefined
          consistencyLevel: consistencyLevel || undefined // If null, set to undefined
        };



      // Call the search service method
      this.milvusService.search(this.searchRequestParams).subscribe(result => {
        console.log('Search Results:', result);
        // Handle the result as needed (e.g., display results)
      }, error => {
        console.error('Search failed:', error);
      });
    } else {
      console.log('Form is invalid:', this.searchForm.errors);
    }
  }

  // Helper method to parse the query vector string into an array
  private parseQueryVector(vectorString: string): number[] {
    var vectorArray = vectorString.split(',').map(value => parseFloat(value.trim())).filter(value => !isNaN(value));
    return vectorArray;
  }

  describeCollection(selectedCollection: string) {
    this.milvusService.describeCollection(selectedCollection).subscribe((result: MilvusCollectionDataModel) => {
      this.collectionStages.IsIndexed = result.isCollectionIndexed;
      this.collectionStages.IsLoaded = result.isCollectionLoaded;
      this.collectionStages.HasData = result.doCollectionHaveData;
      this.selectedCollectionData = result;
      this.initializeFields(); // Make sure this is called after data is fully loaded
      this.onCollectionSelected(selectedCollection);
      this.cdrRef.detectChanges(); // Trigger change detection after initializing fields
    });
  }


  refreshCollectionStatus() {
    if (this.selectedCollectionName) {
      this.describeCollection(this.selectedCollectionName);
    }
  }

  generateDummyArray() {
    const dummyArray = Array.from({length: 513}, () => (Math.random() * 2 - 1).toFixed(4)).join(', ');
    this.arrayField = `[${dummyArray}]`;
  }

  onSubmit() {
    this.isInsertingData = true;
    // Handle form submission logic
    // console.log('Array:', this.arrayField);
    // console.log('Number:', this.numberField);
    // console.log('String:', this.stringField);
    // Add your insert logic here
    console.log(this.form.value)
    this.milvusService.insertData(this.selectedCollectionName, this.vectors).subscribe(() => {
        this.isInsertingData = false;
      },
      (error) => {
        this.isInsertingData = false;
      });
  }

  generateGuid() {
    var id = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });

    this.guidId = id
  }

  setValue(control: AbstractControl<any>, target: any) {
    control.value
  }

  showSchemaData = false;  // Initially set to false to hide schema data
  toggleSchemaData() {
    this.showSchemaData = !this.showSchemaData;  // Toggle the visibility
  }

  protected readonly MilvusDataTypeMapping = MilvusDataTypeMapping;
  protected readonly IndexType = IndexType;
}
