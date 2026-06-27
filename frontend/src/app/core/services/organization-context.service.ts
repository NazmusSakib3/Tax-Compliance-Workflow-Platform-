import { Injectable, computed, inject, signal } from '@angular/core';
import { OrganizationListItem } from '../models/reference-data.models';
import { ReferenceDataApiService } from './reference-data-api.service';
import { AuthService } from './auth.service';
import { isPlatformAdmin } from '../utils/role.utils';

@Injectable({ providedIn: 'root' })
export class OrganizationContextService {
  private readonly storageKey = 'tax-compliance-selected-organization';
  private readonly authService = inject(AuthService);
  private readonly referenceDataApiService = inject(ReferenceDataApiService);
  private readonly selectedOrganizationIdSignal = signal<string | null>(this.readStoredOrganizationId());
  private readonly organizationsSignal = signal<OrganizationListItem[]>([]);

  readonly selectedOrganizationId = computed(() => this.selectedOrganizationIdSignal());
  readonly organizations = computed(() => this.organizationsSignal());
  readonly canSwitchOrganizations = computed(() => isPlatformAdmin(this.authService));

  readonly selectedOrganizationName = computed(() => {
    const organizationId = this.selectedOrganizationIdSignal();
    if (!organizationId) {
      return 'All organizations';
    }

    return this.organizationsSignal().find((organization) => organization.id === organizationId)?.name ?? 'Selected organization';
  });

  loadOrganizations(): void {
    if (!this.canSwitchOrganizations()) {
      this.organizationsSignal.set([]);
      return;
    }

    this.referenceDataApiService.listAllOrganizations().subscribe({
      next: (organizations) => {
        this.organizationsSignal.set(organizations);
        const selectedOrganizationId = this.selectedOrganizationIdSignal();
        if (selectedOrganizationId && !organizations.some((organization) => organization.id === selectedOrganizationId)) {
          this.clearSelection();
        }
      }
    });
  }

  setSelectedOrganizationId(organizationId: string | null): void {
    if (!this.canSwitchOrganizations()) {
      return;
    }

    if (!organizationId) {
      this.clearSelection();
      return;
    }

    localStorage.setItem(this.storageKey, organizationId);
    this.selectedOrganizationIdSignal.set(organizationId);
  }

  clearSelection(): void {
    localStorage.removeItem(this.storageKey);
    this.selectedOrganizationIdSignal.set(null);
  }

  reset(): void {
    this.clearSelection();
    this.organizationsSignal.set([]);
  }

  private readStoredOrganizationId(): string | null {
    return localStorage.getItem(this.storageKey);
  }
}
