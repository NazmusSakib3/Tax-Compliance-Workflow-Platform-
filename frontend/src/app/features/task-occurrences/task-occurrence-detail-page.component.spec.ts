import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TaskOccurrenceApiService } from '../../core/services/task-occurrence-api.service';
import { ComplianceTaskOccurrenceDetail } from '../../core/models/task-occurrence.models';
import { TaskOccurrenceDetailPageComponent } from './task-occurrence-detail-page.component';

describe('TaskOccurrenceDetailPageComponent', () => {
  let fixture: ComponentFixture<TaskOccurrenceDetailPageComponent>;
  let apiService: jasmine.SpyObj<TaskOccurrenceApiService>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    apiService = jasmine.createSpyObj<TaskOccurrenceApiService>('TaskOccurrenceApiService', [
      'getOccurrenceById',
      'getComments',
      'getDocuments',
      'getAuditLog',
      'getAssignableUsers'
    ]);
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole', 'getUserId']);
    apiService.getOccurrenceById.and.returnValue(of(taskOccurrence));
    apiService.getComments.and.returnValue(of([]));
    apiService.getDocuments.and.returnValue(of([]));
    apiService.getAuditLog.and.returnValue(of([]));
    apiService.getAssignableUsers.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [TaskOccurrenceDetailPageComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: taskOccurrence.id }) } }
        },
        { provide: TaskOccurrenceApiService, useValue: apiService },
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();
  });

  it('explains available actions for contributor-only users', () => {
    authService.hasRole.and.callFake((role: string) => role === 'Contributor');
    authService.getUserId.and.returnValue('contributor-1');

    fixture = TestBed.createComponent(TaskOccurrenceDetailPageComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Assigned task');
    expect(text).toContain('You can update status, leave comments, and upload documents for this assignment.');
    expect(apiService.getAssignableUsers).not.toHaveBeenCalled();
  });
});

const taskOccurrence: ComplianceTaskOccurrenceDetail = {
  id: 'occurrence-1',
  complianceTaskRuleId: 'rule-1',
  ruleTitle: 'VAT Return',
  ruleDescription: 'Submit monthly VAT return.',
  legalEntityName: 'Northwind GmbH',
  jurisdictionName: 'Germany',
  templateName: 'VAT Monthly',
  periodStartDate: '2026-04-01',
  periodEndDate: '2026-04-30',
  dueDate: '2026-05-20',
  status: 3,
  assignedToUserId: 'contributor-1',
  assignedToDisplayName: 'Contributor User'
};
