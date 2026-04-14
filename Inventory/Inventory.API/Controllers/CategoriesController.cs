using Inventory.API.Common;
using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Common.Models;
using Inventory.Application.Subcategories.Queries.GetSubcategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public sealed class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(IMediator mediator, ICategoryRepository categoryRepository)
        {
            _mediator = mediator;
            _categoryRepository = categoryRepository;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateCategoryCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(
           ApiResponse<Guid>.Ok(
               id,
               "Categories created successfully"
           ));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateCategoryCommand command)
        {
            if (id != command.Id)
                return BadRequest(
                    ApiResponse<string>.Fail("Id mismatch"));

            var result = await _mediator.Send(command);

            return Ok(
                ApiResponse<Guid>.Ok(
                    result,
                    "Category updated successfully"
                )
            );
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteCategoryCommand(id));

            return Ok(new
            {
                success = true,
                message = "Category deleted successfully"
            });
        }


        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            await _mediator.Send(new BulkDeleteCategoriesCommand(ids));

            return Ok(new
            {
                success = true,
                message = "Category deleted successfully"
            });
        }


        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery(id));
            return result is null ? NotFound() : Ok(result);
        }


        [HttpPost("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> GetCategories(
            [FromBody] GridRequest query)
        {
            var result = await _mediator.Send(
                new GetCategoriesPagedQuery(query));

            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetCategoriesQuery());
            return Ok(result);
        }

        [HttpPost("upload-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload an excel file.");

            var result = await _categoryRepository.UploadCategoriesAsync(file);

            return Ok(new
            {
                Message = $"{result.successCount} Categories uploaded successfully.",
                Errors = result.errors
            });
        }

        [HttpGet("check-duplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(new { exists = false });
            }

            var exists = await _categoryRepository.ExistsByNameAsync(name, excludeId);

            return Ok(new
            {
                exists = exists,
                message = exists ? $"The category name '{name}' is already used by another active category." : string.Empty
            });
        }

    }
}
