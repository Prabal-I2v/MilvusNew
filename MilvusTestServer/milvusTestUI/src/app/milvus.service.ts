import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  IndexCreationParams,
  InsertDataFieldData, InsertDataRequest,
  MilvusCollectionDataModel,
  SearchRequest
} from "./models/MilvusDataModel";

@Injectable({
  providedIn: 'root',
})
export class MilvusService {
  private apiUrl = 'http://localhost:5050/api/milvus';

  constructor(private http: HttpClient) {}

  createCollection(collectionName: string, dimension: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/create-collection?collectionName=${collectionName}&dimension=${dimension}`, {});
  }

  insertData(collectionName: string, data : InsertDataRequest[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/insert-data?collectionName=${collectionName}`, data);
  }

  getTotalData(collectionName: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/total-data?collectionName=${collectionName}`,  {});
  }

  search(requestParams: SearchRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/search`, requestParams); // Adjust the endpoint as needed
  }

  createIndex(params: IndexCreationParams): Observable<any> {
    return this.http.post(`${this.apiUrl}/create-index`, params);
  }

  removeIndex(collectionName: string, columnName: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/remove-index?collectionName=${collectionName}&columnName=${columnName}`, {});
  }

  deleteDataByPrimaryId(collectionName: string, expression : string): Observable<any> {
    return this.http.get(`${this.apiUrl}/deleteDataByPrimaryId?collectionName=${collectionName}&expression=${expression}`, {});
  }

  deleteDataByProperties(collectionName: string, expression : string): Observable<any> {
    return this.http.get(`${this.apiUrl}/deleteDataByProperties?collectionName=${collectionName}&expression=${expression}`, {});
  }
  deleteAllData(collectionName: string, expression : string): Observable<any> {
    return this.http.get(`${this.apiUrl}/deleteAllData?collectionName=${collectionName}&expression=${expression}`, {});
  }
 loadCollection(collectionName: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/load-collection?collectionName=${collectionName}`, {});
  }

  releaseCollection(collectionName: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/release-collection?collectionName=${collectionName}`, {});
  }

  getCollections(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/getAllCollections`);
  }

  describeCollection(collectionName : string): Observable<MilvusCollectionDataModel> {
    return this.http.get<MilvusCollectionDataModel>(`${this.apiUrl}/describe-collection?collectionName=${collectionName}`, {});
  }

  // Add a method to handle connection to Milvus
  connectToMilvus(ip: string, port: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/connect?ip=${ip}&port=${port}`, {});
  }
}
