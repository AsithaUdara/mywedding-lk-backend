// File: src/Presentation/MyWedding.API/Controllers/VendorsController.cs

using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyWedding.Application.Features.Vendors.Queries.GetVendorById;
using MyWedding.Application.Features.Vendors.Queries.GetVendors;

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
        // GET /api/vendors?category=Photography&location=Colombo
        [HttpGet]
        public async Task<IActionResult> GetVendors([FromQuery] GetVendorsQuery query)
        {
            // We pass the entire query object (which includes Category and Location) to MediatR
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
    }
}
