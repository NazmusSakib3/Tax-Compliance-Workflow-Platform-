import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TaskOccurrenceApiService } from '../../core/services/task-occurrence-api.service';
import { ComplianceTaskOccurrenceListItem } from '../../core/models/task-occurrence.models';
import { TaskOccurrencesPageComponent } from './task-occurrences-page.component';

describe('TaskOccurrencesPageComponent', () => {
  let fixture: ComponentFixture<TaskOccurrencesPageComponent>;
  let apiService: jasmine.SpyObj<TaskOccurrenceApiService>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    apiService = jasmine.createSpyObj<TaskOccurrenceApiService>('TaskOccurrenceApiService', ['getOccurrences']);
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole']);
    apiService.getOccurrences.and.returnValue(of({
      items: [taskOccurrence],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1
    }));

    await TestBed.configureTestingModule({
      imports: [TaskOccurrencesPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: () => null
              }
            }
          }
        },
        { provide: TaskOccurrenceApiService, useValue: apiService },
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();
  });

  it('presents assigned work for contributor-only users', () => {
    authService.hasRole.and.callFake((role: string) => role === 'Contributor');

    fixture = TestBed.createComponent(TaskOccurrencesPageComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    const headers = tableHeaders(fixture.nativeElement);

    expect(text).toContain('My Assigned Tasks');
    expect(text).toContain('Only tasks assigned to you are shown here.');
    expect(headers).not.toContain('Assigned To');
    expect(apiService.getOccurrences).toHaveBeenCalledWith(jasmine.objectContaining({ assignedOnly: true }));
  });

  it('shows onboarding hints when contributor task list is empty', () => {
    authService.hasRole.and.callFake((role: string) => role === 'Contributor');
    apiService.getOccurrences.and.returnValue(of({
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 0,
      totalPages: 1
    }));

    fixture = TestBed.createComponent(TaskOccurrencesPageComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Open a task to update its status when you start work.');
  });

  it('shows assignment toggle for multi-role contributors', () => {
    authService.hasRole.and.callFake((role: string) => role === 'Contributor' || role === 'ComplianceManager');

    fixture = TestBed.createComponent(TaskOccurrencesPageComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('All visible tasks');
    expect(text).toContain('My assignments');
  });

  it('keeps assignment context visible for managers', () => {
    authService.hasRole.and.callFake((role: string) => role === 'ComplianceManager');

    fixture = TestBed.createComponent(TaskOccurrencesPageComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    const headers = tableHeaders(fixture.nativeElement);

    expect(text).toContain('Task Occurrences');
    expect(headers).toContain('Assigned To');
  });

  function tableHeaders(nativeElement: HTMLElement): string[] {
    return Array.from(nativeElement.querySelectorAll('th'))
      .map((element) => element.textContent?.trim() ?? '');
  }
});

const taskOccurrence: ComplianceTaskOccurrenceListItem = {
  id: 'occurrence-1',
  complianceTaskRuleId: 'rule-1',
  ruleTitle: 'VAT Return',
  legalEntityName: 'Northwind GmbH',
  jurisdictionName: 'Germany',
  periodStartDate: '2026-04-01',
  periodEndDate: '2026-04-30',
  dueDate: '2026-05-20',
  status: 3,
  assignedToUserId: 'contributor-1',
  assignedToDisplayName: 'Contributor User'
};
