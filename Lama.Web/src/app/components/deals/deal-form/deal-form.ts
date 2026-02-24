import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { DealService } from '../../../services/deal.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-deal-form',
  standalone: false,
  templateUrl: './deal-form.html',
  styleUrl: './deal-form.scss'
})
export class DealForm implements OnInit {
  dealForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private dealService: DealService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.dealForm = this.fb.group({
      name: ['', [Validators.required]],
      companyId: ['', [Validators.required]],
      amount: [0, [Validators.required, Validators.min(0)]],
      expectedCloseDate: ['', [Validators.required]],
      pipelineId: ['', [Validators.required]],
      stageId: ['', [Validators.required]],
      currency: ['USD'],
      contactId: [''],
      description: [''],
      ownerId: ['']
    });
  }

  onSubmit(): void {
    if (this.dealForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      // Clean up empty strings - convert to null or remove them
      const formData = this.cleanFormData(this.dealForm.value);

      this.dealService.create(formData).subscribe({
        next: (id) => {
          this.toastService.success('Deal created successfully!');
          this.router.navigate(['/deals']);
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
    this.router.navigate(['/deals']);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.dealForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.dealForm.get(fieldName);
    if (field?.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    if (field?.hasError('min')) {
      return `${this.getFieldLabel(fieldName)} must be greater than or equal to 0`;
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      'name': 'Deal name',
      'companyId': 'Company',
      'amount': 'Amount',
      'expectedCloseDate': 'Expected close date',
      'pipelineId': 'Pipeline',
      'stageId': 'Stage'
    };
    return labels[fieldName] || fieldName;
  }
}