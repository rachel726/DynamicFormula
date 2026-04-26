// ╔══════════════════════════════════════════════════════════════════╗
// ║  data.service.ts                                                  ║
// ║  Dual-mode: בסביבת dev → Web API | בסביבת prod → JSON סטטי      ║
// ║                                                                   ║
// ║  Dev:  Angular → http://localhost:5000/api  (DynamicFormula.Api) ║
// ║  Prod: Angular → assets/data/report.json    (GitHub Pages)       ║
// ╚══════════════════════════════════════════════════════════════════╝
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PerformanceRow {
  targil_id:   number;
  description: string;
  targil:      string;
  sql_time:    number | null;
  csharp_time: number | null;
  nodejs_time: number | null;
}

export interface Formula {
  targilId:    number;
  targil:      string;
  tnai:        string | null;
  targilFalse: string | null;
  description: string;
  isConditional: boolean;
}

@Injectable({ providedIn: 'root' })
export class DataService {

  constructor(private http: HttpClient) {}

getReport(): Promise<PerformanceRow[]> {
    const url = environment.apiUrl
      ? `${environment.apiUrl}/performance`
      : 'assets/data/report.json';
    console.log('📊 Loading report from:', url);
    return firstValueFrom(this.http.get<PerformanceRow[]>(url));
}

getFormulas(): Promise<Formula[]> {
    const url = environment.apiUrl
      ? `${environment.apiUrl}/formulas`
      : 'assets/data/formulas.json';
    console.log('📋 Loading formulas from:', url);
    return firstValueFrom(this.http.get<Formula[]>(url));
}
}
