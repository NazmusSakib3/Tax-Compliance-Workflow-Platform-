import { ConfirmDialogService } from './confirm-dialog.service';

describe('ConfirmDialogService', () => {
  let service: ConfirmDialogService;

  beforeEach(() => {
    service = new ConfirmDialogService();
  });

  it('exposes the pending request while a confirm is open', () => {
    void service.confirm({ message: 'Delete this?' });

    expect(service.request()).toEqual(jasmine.objectContaining({ message: 'Delete this?' }));
  });

  it('resolves the promise with true and clears the request when confirmed', async () => {
    const result = service.confirm({ message: 'Proceed?' });

    service.respond(true);

    await expectAsync(result).toBeResolvedTo(true);
    expect(service.request()).toBeNull();
  });

  it('resolves the promise with false when cancelled', async () => {
    const result = service.confirm({ message: 'Proceed?' });

    service.respond(false);

    await expectAsync(result).toBeResolvedTo(false);
  });

  it('ignores responses when there is no active request', () => {
    expect(() => service.respond(true)).not.toThrow();
    expect(service.request()).toBeNull();
  });
});
