import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {IndexCreationParams, MilvusCollectionDataModel, SearchRequest} from "./models/MilvusDataModel";

@Injectable({
  providedIn: 'root',
})
export class MilvusService {
  private apiUrl = 'http://localhost:5050/api/milvus';

  constructor(private http: HttpClient) {}

  createCollection(collectionName: string, dimension: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/create-collection?collectionName=${collectionName}&dimension=${dimension}`, {});
  }

  insertData(collectionName: string, vectors: number[][]): Observable<any> {
    return this.http.post(`${this.apiUrl}/insert-data?collectionName=${collectionName}`, vectors);
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
