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

        //access datacontext
        public ValuesController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetValues()
        {
            //get values from db
            var values = await dataContext.Values.ToListAsync();

            //return them
            return Ok(values);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetValue(int id)
        {
            //get single value from db
            var value = await dataContext.Values.FirstOrDefaultAsync(x => x.Id == id);

            //return it
            return Ok(value);
        }

        [HttpGet("strings")]
        public ActionResult<IEnumerable<string>> GetStringArray()
        {
            //return array of strings
            return new string[] { "ერთი", "ორი" };
        }

        [HttpGet("strings/{id}")]
        public ActionResult<string> GetSingleString(int id)
        {
            //return one string
            return "მხოლოდ ერთი სტრინგი";
        }
    }
}
