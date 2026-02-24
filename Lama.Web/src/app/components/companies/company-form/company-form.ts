import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CompanyService } from '../../../services/company.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-company-form',
  standalone: false,
  templateUrl: './company-form.html',
  styleUrl: './company-form.scss'
})
export class CompanyForm implements OnInit {
  companyForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private companyService: CompanyService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.companyForm = this.fb.group({
      name: ['', [Validators.required]],
      domain: [''],
      industry: [''],
      website: [''],
      clientCategoryId: ['']
    });
  }

  onSubmit(): void {
    if (this.companyForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      // Clean up empty strings - convert to null or remove them
      const formData = this.cleanFormData(this.companyForm.value);

      this.companyService.create(formData).subscribe({
        next: (id) => {
          this.toastService.success('Company created successfully!');
          this.router.navigate(['/companies']);
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
    this.router.navigate(['/companies']);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.companyForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.companyForm.get(fieldName);
    if (field?.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      'name': 'Company name'
    };
    return labels[fieldName] || fieldName;
  }
}