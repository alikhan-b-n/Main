export interface DealDto {
  id: string;
  name: string;
  description?: string;
  companyId: string;
  contactId?: string;
  amount: number;
  currency: string;
  probability: number;
  expectedCloseDate: string;
  actualCloseDate?: string;
  pipelineId: string;
  stageId: string;
  ownerId?: string;
  createdAt: string;
}

export interface CreateDealCommand {
  name: string;
  companyId: string;
  amount: number;
  expectedCloseDate: Date;
  pipelineId: string;
  stageId: string;
  currency?: string;
  contactId?: string;
  description?: string;
  ownerId?: string;
}
