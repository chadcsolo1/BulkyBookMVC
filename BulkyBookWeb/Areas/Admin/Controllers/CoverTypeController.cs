using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Index() will retrieve all of the cover types available from the DB
        // and display them in a table on the cover type view
        public IActionResult Index()
        {
            IEnumerable<CoverType> objCoverTypeList = _unitOfWork.CoverType.GetAll();
            return View(objCoverTypeList);
        }

        //GET
        public IActionResult Create()
        {
            return View();
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType objCoverType)
        {
            //Create some validation to check that the name field is not blank
            if (objCoverType.Name == null)
            {
                ModelState.AddModelError("name", "The Name cannot be empty!");

            }
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Add(objCoverType);

                _unitOfWork.Save();

                TempData["success"] = "Cover Type created successfully!";

                return RedirectToAction("Index");
            }

            return View();
        }

        //GET
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var coverTypeFromDbFirst = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

            if (coverTypeFromDbFirst == null)
            {
                return NotFound();
            }

            return View(coverTypeFromDbFirst);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Edit(CoverType objCoverType)
        {
            //Check to make sure the Name field is not null
            if (objCoverType.Name == null)
            {
                ModelState.AddModelError("name", "The Name field cannot be empty!");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Update(objCoverType);

                _unitOfWork.Save();

                TempData["Success"] = "Category Updated Successfully!";

                return RedirectToAction("Index");
            }

            return View(objCoverType);

        }

        //GET
        public IActionResult Delete(int? id)
        {
            //Validate the id is not null or 0
            if(id == null || id == 0) 
            {
                return NotFound();
            }

            var coverTypeFromDbFirts = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

            if(coverTypeFromDbFirts == null)
            {
                return NotFound();
            }

            return View(coverTypeFromDbFirts);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var objCoverTypeDelete = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id==id);


            if(objCoverTypeDelete == null)
            {
                return NotFound();
            }
            
            _unitOfWork.CoverType.Delete(objCoverTypeDelete);

            _unitOfWork.Save();

            TempData["Success"] = "Cover Type deleted Successfully!";

            return RedirectToAction("Index");
        }
        

       


    }


 }
