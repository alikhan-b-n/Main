import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DealDto, CreateDealCommand } from '../models/deal.model';

@Injectable({
  providedIn: 'root'
})
export class DealService {
  private apiUrl = `${environment.apiUrl}/crm/objects/deals`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DealDto[]> {
    return this.http.get<DealDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<DealDto> {
    return this.http.get<DealDto>(`${this.apiUrl}/${id}`);
  }

  create(command: CreateDealCommand): Observable<string> {
    return this.http.post<string>(this.apiUrl, command);
  }
}
