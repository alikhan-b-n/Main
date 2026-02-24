import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TicketService } from '../../../services/ticket.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-ticket-form',
  standalone: false,
  templateUrl: './ticket-form.html',
  styleUrl: './ticket-form.scss'
})
export class TicketForm implements OnInit {
  ticketForm!: FormGroup;
  isSubmitting = false;

  statusOptions = ['Open', 'In_Progress', 'Resolved', 'Closed'];
  priorityOptions = ['Low', 'Medium', 'High', 'Urgent'];
  sourceOptions = ['Email', 'Phone', 'Web', 'Chat', 'Social'];

  constructor(
    private fb: FormBuilder,
    private ticketService: TicketService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.ticketForm = this.fb.group({
      ticketName: ['', [Validators.required]],
      description: ['', [Validators.required]],
      status: ['Open', [Validators.required]],
      priority: ['Medium', [Validators.required]],
      source: ['Web', [Validators.required]],
      contactId: ['', [Validators.required]],
      pipelineId: ['', [Validators.required]],
      stageId: ['', [Validators.required]],
      companyId: [''],
      ownerId: ['']
    });
  }

  onSubmit(): void {
    if (this.ticketForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      // Clean up empty strings - convert to null or remove them
      const formData = this.cleanFormData(this.ticketForm.value);

      this.ticketService.create(formData).subscribe({
        next: (id) => {
          this.toastService.success('Ticket created successfully!');
          this.router.navigate(['/tickets']);
        },
        error: (error) => {
          // Error already handled by interceptor
          this.isSubmitting = false;
        }
      });
    }
  }

  private cleanFormData(data: any): any {
    const cleaned: any = {};
    for (const key in data) {
      if (data[key] !== '' && data[key] !== null && data[key] !== undefined) {
        cleaned[key] = data[key];
      }
    }
    return cleaned;
  }

  cancel(): void {
    this.router.navigate(['/tickets']);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.ticketForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.ticketForm.get(fieldName);
    if (field?.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      'ticketName': 'Ticket name',
      'description': 'Description',
      'status': 'Status',
      'priority': 'Priority',
      'source': 'Source',
      'contactId': 'Contact',
      'pipelineId': 'Pipeline',
      'stageId': 'Stage'
    };
    return labels[fieldName] || fieldName;
  }
}