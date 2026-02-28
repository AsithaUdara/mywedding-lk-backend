using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Vendors.Queries.GetVendorById;
using MyWedding.Application.Features.Vendors.Queries.GetVendors;
using MyWedding.Application.Features.Vendors.Commands.RegisterVendor;
using System;
using System.Threading.Tasks;

namespace MyWedding.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VendorsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /api/vendors
        [HttpGet]
        public async Task<IActionResult> GetVendors([FromQuery] GetVendorsQuery query)
        {
            var vendors = await _mediator.Send(query);
            return Ok(vendors);
        }

        // GET /api/vendors/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVendorById(string id)
        {
            var query = new GetVendorByIdQuery { VendorId = id };
            var vendor = await _mediator.Send(query);

            return vendor is not null ? Ok(vendor) : NotFound();
        }

        // POST /api/vendors/register
        [HttpPost("register")]
        public async Task<IActionResult> RegisterVendor([FromBody] RegisterVendorCommand command)
        {
            try 
            {
                await _mediator.Send(command);
                return Ok(new { success = true, vendorId = command.UserId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }
}
