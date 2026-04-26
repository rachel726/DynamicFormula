// ╔══════════════════════════════════════════════════════════════════╗
// ║  dashboard.component.ts                                           ║
// ║  ────────────────────────────────────────────────                ║
// ║  מסך דוח מסכם — השוואת ביצועים בין 3 שיטות                     ║
// ║  SQL (Stored Proc) · C# (Compiled Lambda) · Node.js (math.js)   ║
// ╚══════════════════════════════════════════════════════════════════╝
import { Component, ElementRef, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule }     from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

import { DataService, PerformanceRow, Formula } from '../services/data.service';

@Component({
  selector:    'app-dashboard',
  standalone:  true,
  imports:     [CommonModule, HttpClientModule],
  templateUrl: './dashboard.component.html',
  styleUrls:   ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, AfterViewInit {

  @ViewChild('chart') chartRef!: ElementRef<HTMLCanvasElement>;

  rows:      PerformanceRow[] = [];
  formulas:  Formula[] = [];
  isLoading = true;
  error     = '';

  constructor(private data: DataService) {}

  async ngOnInit(): Promise<void> {
    try {
      [this.rows, this.formulas] = await Promise.all([
        this.data.getReport(),
        this.data.getFormulas()
      ]);
    } catch (e: any) {
      this.error = `Failed to load report: ${e?.message ?? e}`;
    }
    this.isLoading = false;
    setTimeout(() => this.drawChart(), 200);
  }

ngAfterViewInit(): void {}

// ngAfterViewChecked(): void {
//     if (this.rows.length > 0 && !this.chartDrawn && this.chartRef) {
//         this.chartDrawn = true;
//         this.drawChart();
//     }
// }



private drawChart(): void {
    if (this.rows.length === 0) return;
    
    const container = document.getElementById('chart-container');
    if (!container) return;
    
    const maxVal = Math.max(...this.rows.map(r => r.sql_time ?? 0));
    
    const logScale = (v: number) => Math.max(6, (Math.log(1 + v) / Math.log(1 + maxVal)) * 230);

    let html = '<div style="display:flex;flex-direction:column;gap:0">';
    html += '<div style="display:flex;gap:6px;align-items:flex-end;height:260px;padding:16px 16px 0">';

    for (const row of this.rows) {
        const sqlH  = logScale(row.sql_time    ?? 0).toFixed(0);
        const csH   = logScale(row.csharp_time ?? 0).toFixed(0);
        const nodeH = logScale(row.nodejs_time ?? 0).toFixed(0);

        const tipStyle = `position:absolute;bottom:calc(100% + 6px);left:50%;transform:translateX(-50%);
          background:#1e293b;border:1px solid #475569;border-radius:6px;padding:8px 10px;
          font-size:11px;color:#f1f5f9;white-space:nowrap;z-index:99;display:none;
          line-height:1.8;text-align:right;direction:rtl;box-shadow:0 4px 12px rgba(0,0,0,0.4)`;

        html += `
        <div style="position:relative;display:flex;gap:2px;align-items:flex-end;cursor:pointer"
             onmouseenter="this.querySelector('.tip').style.display='block'"
             onmouseleave="this.querySelector('.tip').style.display='none'">
          <div class="tip" style="${tipStyle}">
            <span style="color:#14b8a6">■</span> SQL: ${row.sql_time?.toFixed(3)} שנ'<br>
            <span style="color:#8b5cf6">■</span> C#: ${row.csharp_time?.toFixed(3)} שנ'<br>
            <span style="color:#f97316">■</span> Node.js: ${row.nodejs_time?.toFixed(3)} שנ'
          </div>
          <div style="width:12px;height:${sqlH}px;background:#14b8a6;border-radius:2px 2px 0 0"></div>
          <div style="width:12px;height:${csH}px;background:#8b5cf6;border-radius:2px 2px 0 0"></div>
          <div style="width:12px;height:${nodeH}px;background:#f97316;border-radius:2px 2px 0 0"></div>
        </div>`;
    }

    html += '</div>';
    html += `
      <div style="display:flex;justify-content:center;gap:24px;padding:12px;border-top:1px solid #334155;margin-top:8px">
        <span style="display:flex;align-items:center;gap:6px;font-size:12px;color:#94a3b8">
          <span style="width:12px;height:12px;background:#14b8a6;border-radius:2px;display:inline-block"></span>SQL
        </span>
        <span style="display:flex;align-items:center;gap:6px;font-size:12px;color:#94a3b8">
          <span style="width:12px;height:12px;background:#8b5cf6;border-radius:2px;display:inline-block"></span>C# .NET
        </span>
        <span style="display:flex;align-items:center;gap:6px;font-size:12px;color:#94a3b8">
          <span style="width:12px;height:12px;background:#f97316;border-radius:2px;display:inline-block"></span>Node.js
        </span>
        <span style="font-size:11px;color:#64748b;align-self:center">* סקאלה לוגריתמית · רחף לצפייה בערך</span>
      </div>
    </div>`;
    container.innerHTML = html;
}

  // ───── Helpers for template ─────
  fullFormula(row: PerformanceRow): string {
    const f = this.formulas.find(x => x.targilId === row.targil_id);
    if (f?.isConditional && f.tnai && f.targilFalse) {
      return `IF(${f.tnai}, ${f.targil}, ${f.targilFalse})`;
    }
    return row.targil;
  }

  fmt(n: number | null | undefined, digits = 3): string {
    if (n == null) return '—';
    return n.toFixed(digits);
  }

  fastest(row: PerformanceRow): string {
    const v: Array<[string, number | null]> = [
      ['SQL',     row.sql_time],
      ['C#',      row.csharp_time],
      ['Node.js', row.nodejs_time]
    ];
    const valid = v.filter(([, t]) => t != null) as Array<[string, number]>;
    if (valid.length === 0) return '—';
    valid.sort((a, b) => a[1] - b[1]);
    return valid[0][0];
  }

  get summary() {
    const avg = (pick: (r: PerformanceRow) => number | null) => {
      const vals = this.rows.map(pick).filter(x => x != null) as number[];
      return vals.length > 0 ? vals.reduce((a, b) => a + b, 0) / vals.length : null;
    };
    const sum = (pick: (r: PerformanceRow) => number | null) => {
      const vals = this.rows.map(pick).filter(x => x != null) as number[];
      return vals.length > 0 ? vals.reduce((a, b) => a + b, 0) : null;
    };
    return {
      sql:       avg(r => r.sql_time),
      csharp:    avg(r => r.csharp_time),
      nodejs:    avg(r => r.nodejs_time),
      sqlTotal:  sum(r => r.sql_time),
      csTotal:   sum(r => r.csharp_time),
      nodeTotal: sum(r => r.nodejs_time),
    };
  }

  get rankedSummary() {
    const s = this.summary;
    const items = [
      { name: 'SQL',     value: s.sql,    cls: 'sql' },
      { name: 'C# .NET', value: s.csharp, cls: 'cs'  },
      { name: 'Node.js', value: s.nodejs, cls: 'ts'  }
    ];
    const valid = items.filter(x => x.value != null);
    valid.sort((a, b) => (a.value ?? Infinity) - (b.value ?? Infinity));
    return valid;
  }
}

