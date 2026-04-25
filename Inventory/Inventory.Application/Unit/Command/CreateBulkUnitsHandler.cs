using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Units.Command
{
    public class CreateBulkUnitsHandler : IRequestHandler<CreateBulkUnitsCommand, bool>
    {
        private readonly IUnitRepository _repo;
        private readonly IUnitOfWork _uow; // Transaction handle karne ke liye

        public CreateBulkUnitsHandler(IUnitRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<bool> Handle(CreateBulkUnitsCommand request, CancellationToken ct)
        {
            // 1. Pre-fetch tracked entities for upsert logic
            var dbUnits = await _repo.Query().ToListAsync(ct);
            var unitsByName = dbUnits.ToDictionary(u => u.Name.ToLower().Trim(), u => u);
            
            // Track duplicates within the same batch
            var processedNames = new HashSet<string>();

            foreach (var item in request.Units)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;
                
                var nameKey = item.Name.ToLower().Trim();
                if (processedNames.Contains(nameKey)) continue; // Skip if duplicated in batch
                processedNames.Add(nameKey);

                if (unitsByName.TryGetValue(nameKey, out var existingUnit))
                {
                    // UPDATE
                    existingUnit.Update(item.Name, item.Description, true, request.CompanyId, request.BranchId);
                }
                else
                {
                    // INSERT
                    var unit = new UnitMaster(item.Name, item.Description, request.CompanyId, request.BranchId);
                    await _repo.AddAsync(unit);
                }
            }

            return await _uow.SaveChangesAsync(ct) > 0; 
        }
    }

    public class UpdateUnitHandler : IRequestHandler<UpdateUnitCommand, bool>
    {
        private readonly IUnitRepository _repo;
        private readonly IUnitOfWork _uow;

        public UpdateUnitHandler(IUnitRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<bool> Handle(UpdateUnitCommand request, CancellationToken ct)
        {
            var unit = await _repo.GetByIdAsync(request.Id);
            if (unit == null) return false;

            unit.Update(request.Name, request.Description, request.IsActive, request.CompanyId, request.BranchId);
            await _repo.UpdateAsync(unit);
            return await _uow.SaveChangesAsync(ct) > 0;
        }
    }

    public class DeleteUnitHandler : IRequestHandler<DeleteUnitCommand, bool>
    {
        private readonly IUnitRepository _repo;
        private readonly IUnitOfWork _uow;

        public DeleteUnitHandler(IUnitRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<bool> Handle(DeleteUnitCommand request, CancellationToken ct)
        {
            await _repo.DeleteAsync(request.Id);
            return await _uow.SaveChangesAsync(ct) > 0;
        }
    }
}
