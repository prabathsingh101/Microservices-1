# Antigravity Rules for Microservices Workspace

## 1. Multi-Tenant CompanyId Propagation in Consumers
* **Rule**: When processing events in background workers or RabbitMQ consumers, the `HttpContext` is not available, meaning `_currentUserService.CompanyId` is always `null`.
* **Action**: Always propagate the `CompanyId` sent in the event message/DTO payload:
  ```csharp
  CompanyId = (dto.CompanyId != null && dto.CompanyId != Guid.Empty) 
              ? dto.CompanyId 
              : _currentUserService.CompanyId
  ```
  Never overwrite a non-empty `CompanyId` received from the DTO with `_currentUserService.CompanyId`.

## 2. API Gateway Routing & URL Structure
* **Rule**: The YARP Gateway maps routes by forwarding `/api/company/{**catch-all}` to `api/{**catch-all}` on the Company service.
* **Action**: Do not change double-prefixed paths like `/api/company/company/profile` to `/api/company/profile` on the frontend, because the backend controller has the route `api/Company/profile`. Changing it will result in `404 Not Found`.
