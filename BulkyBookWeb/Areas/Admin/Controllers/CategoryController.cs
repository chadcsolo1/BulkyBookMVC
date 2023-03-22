using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        //Dependency Injection For the DbContext.
        //THis allows us to call on the DB Object using '_db.'
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            //We use the IEnumerable class to return a list of Category objects
            //The below is an alternative to line 22, and just return View(objCategories)
            //var objCategories = _db.Categories.ToList();
            IEnumerable<category> objCategoryList = _unitOfWork.Category.GetAll();
            return View(objCategoryList);
        }

        //GET
        public IActionResult Create()
        {

            return View();
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The Name cannot be the same as the display order");
            }
            if (ModelState.IsValid)
            {
                //Opens a seesion with the DB to add the 'obj' parameter
                _unitOfWork.Category.Add(obj);
                //The changes to the DB are saved with the line of code
                _unitOfWork.Save();
                TempData["success"] = "Category Created Successfully!";
                //Redirects the to the Index action where an updated list of categories will be displayed
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        //GET
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            //var categoryFromDb = _db.Categories.Find(id);
            //**** These are a couple of options to find the category matching the id parameter entered for theis method.
            //**** Find() is just the simplest so we used it.
            var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);
            //var categoryFromDbSingle = _db.Categories.SingleOrDefault(u => u.Id == id);

            if (categoryFromDbFirst == null)
            {
                return NotFound();
            }
            return View(categoryFromDbFirst);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "The Name cannot be the same as the display order");
            }
            if (ModelState.IsValid)
            {
                //Opens a seesion with the DB to Update the 'obj' parameter
                _unitOfWork.Category.Update(obj);
                //The changes to the DB are saved with the line of code
                _unitOfWork.Save();
                TempData["success"] = "Category Updated Successfully";
                //Redirects the to the Index action where an updated list of categories will be displayed
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        //GET
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            //var categoryFromDb = _db.Categories.Find(id);
            //**** These are a couple of options to find the category matching the id parameter entered for theis method.
            //**** Find() is just the simplest so we used it.
            var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);
            //var categoryFromDbSingle = _db.Categories.SingleOrDefault(u => u.Id == id);
            if (categoryFromDbFirst == null)
            {
                return NotFound();
            }
            return View(categoryFromDbFirst);
        }

        //POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {

            var obj = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);

            if (obj == null)
            {
                return NotFound();
            }

            _unitOfWork.Category.Delete(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");


        }
    }
}
