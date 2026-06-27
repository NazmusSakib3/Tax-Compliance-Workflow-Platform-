import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { OrganizationListItem } from '../models/reference-data.models';
import { AuthService } from './auth.service';
import { OrganizationContextService } from './organization-context.service';
import { ReferenceDataApiService } from './reference-data-api.service';

describe('OrganizationContextService', () => {
  let service: OrganizationContextService;
  let authService: jasmine.SpyObj<AuthService>;
  let referenceDataApiService: jasmine.SpyObj<ReferenceDataApiService>;

  const organizations: OrganizationListItem[] = [
    { id: 'org-1', name: 'Northwind', code: 'NW', isActive: true, legalEntityCount: 2 }
  ];

  beforeEach(() => {
    localStorage.clear();
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole', 'getOrganizationId']);
    referenceDataApiService = jasmine.createSpyObj<ReferenceDataApiService>('ReferenceDataApiService', ['listAllOrganizations']);
    referenceDataApiService.listAllOrganizations.and.returnValue(of(organizations));

    TestBed.configureTestingModule({
      providers: [
        OrganizationContextService,
        { provide: AuthService, useValue: authService },
        { provide: ReferenceDataApiService, useValue: referenceDataApiService }
      ]
    });

    service = TestBed.inject(OrganizationContextService);
  });

  it('loads organizations for platform admins and persists selection', () => {
    authService.hasRole.and.returnValue(true);
    authService.getOrganizationId.and.returnValue(undefined);

    service.loadOrganizations();
    service.setSelectedOrganizationId('org-1');

    expect(service.organizations()).toEqual(organizations);
    expect(service.selectedOrganizationId()).toBe('org-1');
    expect(service.selectedOrganizationName()).toBe('Northwind');
  });

  it('clears invalid stored organization selections', () => {
    authService.hasRole.and.returnValue(true);
    authService.getOrganizationId.and.returnValue(undefined);
    localStorage.setItem('tax-compliance-selected-organization', 'missing-org');

    service.loadOrganizations();

    expect(service.selectedOrganizationId()).toBeNull();
  });
});
