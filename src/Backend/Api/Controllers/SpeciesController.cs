using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Persistence;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpeciesController : ControllerBase
    {
        private IRepository<AntSpecies> _repository => RepositoryFactory.GetRepository();

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _repository.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AntSpecies data)
        {
            await _repository.AddAsync(data);
            return Ok();
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<AntSpecies> data)
        {
            await _repository.AddRangeAsync(data);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AntSpecies data)
        {
            await _repository.UpdateAsync(data);
            return Ok();
        }

        // Nueva ruta: descripción detallada de una especie (si existe)
        [HttpGet("{id}/description")]
        public async Task<IActionResult> GetDescription(int id)
        {
            var desc = await _repository.GetDescriptionAsync(id);
            if (string.IsNullOrWhiteSpace(desc))
                return NotFound("No hay descripción para esta especie.");
            return Ok(new { Description = desc });
        }
    }
}
