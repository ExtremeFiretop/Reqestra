import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PlexOAuthService } from './plexoauth.service';
import { of } from 'rxjs';

function createService() {
  const mockHttp = {
    post: vi.fn().mockReturnValue(of({})),
  };
  const service = Object.create(PlexOAuthService.prototype);
  service.http = mockHttp;
  service.url = '/api/v1/PlexOAuth/';
  service.headers = { set: vi.fn() };
  return { service: service as PlexOAuthService, mockHttp };
}

describe('PlexOAuthService', () => {
  let service: PlexOAuthService;
  let mockHttp: ReturnType<typeof createService>['mockHttp'];

  beforeEach(() => {
    const mocks = createService();
    service = mocks.service;
    mockHttp = mocks.mockHttp;
  });

  it('should POST the opaque poll token in the request body', () => {
    const pollToken = 'a'.repeat(64);
    service.oAuth(pollToken);
    expect(mockHttp.post).toHaveBeenCalledWith('/api/v1/PlexOAuth/', { pollToken }, expect.anything());
  });
});
