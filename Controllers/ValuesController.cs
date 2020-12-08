using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MijnuriAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MijnuriAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly DataContext dataContext;

        public ValuesController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetValues()
        {
            var values = await dataContext.Values.ToListAsync();

            return Ok(values);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetValue(int id)
        {
            var value = await dataContext.Values.FirstOrDefaultAsync(x => x.Id == id);

            return Ok(value);
        }

        [HttpGet("strings")]
        public ActionResult<IEnumerable<string>> GetStringArray()
        {
            return new string[] { "ერთი", "ორი" };
        }

        [HttpGet("strings/{id}")]
        public ActionResult<string> GetSingleString(int id)
        {
            return "მხოლოდ ერთი სტრინგი";
        }
    }
}
