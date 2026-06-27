export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
}
