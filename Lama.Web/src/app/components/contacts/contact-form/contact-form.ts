import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ContactService } from '../../../services/contact.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-contact-form',
  standalone: false,
  templateUrl: './contact-form.html',
  styleUrl: './contact-form.scss'
})
export class ContactForm implements OnInit {
  contactForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private contactService: ContactService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.contactForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      jobTitle: ['']
    });
  }

  onSubmit(): void {
    if (this.contactForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      // Clean up empty strings - convert to null or remove them
      const formData = this.cleanFormData(this.contactForm.value);

      this.contactService.create(formData).subscribe({
        next: (id) => {
          this.toastService.success('Contact created successfully!');
          this.router.navigate(['/contacts']);
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
    this.router.navigate(['/contacts']);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.contactForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.contactForm.get(fieldName);
    if (field?.hasError('required')) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    if (field?.hasError('email')) {
      return 'Invalid email address';
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      'firstName': 'First name',
      'lastName': 'Last name',
      'email': 'Email'
    };
    return labels[fieldName] || fieldName;
  }
}
