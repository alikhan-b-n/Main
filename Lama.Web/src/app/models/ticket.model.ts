export interface TicketDto {
  id: string;
  ticketName: string;
  description: string;
  status: string;
  priority: string;
  source: string;
  contactId: string;
  companyId?: string;
  pipelineId: string;
  stageId: string;
  ownerId?: string;
  createdAt: string;
  closedAt?: string;
}

export interface CreateTicketCommand {
  ticketName: string;
  description: string;
  status: string;
  priority: string;
  source: string;
  contactId: string;
  pipelineId: string;
  stageId: string;
  companyId?: string;
  ownerId?: string;
}
