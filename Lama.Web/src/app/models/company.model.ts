export interface CompanyDto {
  id: string;
  name: string;
  domain?: string;
  industry?: string;
  website?: string;
  clientCategoryId?: string;
  totalSpent: number;
  createdAt: string;
  lastActivityAt: string;
}

export interface CreateCompanyCommand {
  name: string;
  industry?: string;
  website?: string;
  domain?: string;
  clientCategoryId?: string;
}
