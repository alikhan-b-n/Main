export interface ContactDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  jobTitle?: string;
  companyId?: string;
  ownerId?: string;
  lifecycleStage?: string;
  createdAt: string;
  lastActivityAt: string;
}

export interface CreateContactCommand {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  jobTitle?: string;
  companyId?: string;
  ownerId?: string;
  lifecycleStage?: string;
}
