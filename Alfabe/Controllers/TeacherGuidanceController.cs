using System;
using Alfabe.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfabe.Controllers
{

    [Authorize(Roles = "Teacher,Admin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TeacherGuidanceController : Controller
    {
        private readonly AlfabeContext _ctx;

        public TeacherGuidanceController(AlfabeContext ctx)
        {
            _ctx = ctx;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
