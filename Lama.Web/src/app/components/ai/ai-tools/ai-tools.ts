import { Component } from '@angular/core';
import { AiService } from '../../../services/ai.service';
import { ActivitySummaryDto } from '../../../models/ai.model';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-ai-tools',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-tools.html',
  styleUrls: ['./ai-tools.scss']
})
export class AiTools {
  activityId = '';
  loading = false;
  result: ActivitySummaryDto | null = null;
  error: string | null = null;

  constructor(private aiService: AiService) {}

  summarize(): void {
    if (!this.activityId) {
      this.error = 'Please provide an activity ID (GUID)';
      return;
    }

    this.loading = true;
    this.result = null;
    this.error = null;

    this.aiService.summarizeActivity(this.activityId).subscribe({
      next: (res) => {
        this.result = res;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.message || (err?.message ?? 'Unknown error');
        this.loading = false;
      }
    });
  }
}
