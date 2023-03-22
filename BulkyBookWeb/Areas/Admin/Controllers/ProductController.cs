using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing.Constraints;
using NuGet.Packaging.Signing;
using System.Collections.Generic;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            

            return View();
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            ProductViewModel productVM = new()
            {
                Product = new(),
                CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() }),

                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() }),
            };


            if (id == null || id == 0)
            {
                //Create Product

                return View(productVM);
            }
            else
            {
                //Update Product
                productVM.Product = _unitOfWork.Product.GetFirstOrDefault(i => i.Id == id);
                return View(productVM);
            }

            
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                //wwwRootPath holds the file path of the wwwroot folder
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if(file != null)
                {
                    //Guid will assign the string variable fileName a globally unique identifier
                    string fileName = Guid.NewGuid().ToString();

                    //Path performs operations on string instances that conatin file or directory path information
                    var uploads = Path.Combine(wwwRootPath, @"images\products");

                    //GetExtensions returns the extensions of specified path string.
                    //Extensions are fileName.jpeg, fileName.doc, fileName.pdf, fileName.xls. This information is important so the application
                    // Knows what programs to do with these files. For Example, will the file contain text or an image
                    var extensions = Path.GetExtension(file.FileName);

                    //Check if Image already exist in database for this product so we can delete it
                    if(obj.Product.ImageUrl != null)
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    //FileStream class allows us to easily read and write data into a file. Stream file class provides a stream for file operations.
                    using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extensions), FileMode.Create))
                    {
                        file.CopyTo(fileStreams);
                    }
                    obj.Product.ImageUrl=@"\images\products" + fileName + extensions;
                   

                }

                if(obj.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(obj.Product);
                } else
                {
                    _unitOfWork.Product.Update(obj.Product);
                }

                    
                    _unitOfWork.Save();
                    TempData["success"] = "Product Created Successfully";
                    return RedirectToAction("Index");

            }
               return View(obj); 

        }

        //Delete
        //public IActionResult Delete(int? id)
        //{
        //    if(id == null || id == 0)
        //    {
        //        return NotFound();
        //    }

        //    var categoryFromDbFirst = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id);
        //    if(categoryFromDbFirst == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(categoryFromDbFirst);

            
        //}

        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public IActionResult DeleteProduct(int? id)
        //{
        //    var obj = _unitOfWork.Product.GetFirstOrDefault(u => u.Id==id);
        //    if(obj == null)
        //    {
        //        return Json(new { success = false, message = "Error while deleting product"});
        //    }

        //    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
        //    if(System.IO.File.Exists(oldImagePath))
        //    {
        //        System.IO.File.Delete(oldImagePath);
        //    }

        //    _unitOfWork.Product.Delete(obj);
        //    _unitOfWork.Save();
        //    return Json(new { success = true, message = "Product was deleted successfully"});

        //    return RedirectToAction("Index");
        //}

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfWork.Product.GetAll(includeProperties:"Category,CoverType");
            return Json(new {data = productList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Product.GetFirstOrDefault(u => u.Id==id);
            if(obj == null)
            {
                return Json(new { success = false, message = "Error while deleting product"});
            }

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if(System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _unitOfWork.Product.Delete(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Product was deleted successfully"});

        }
        #endregion

    }
        
        
    
}

                
            
        
        
            
        
            //if (ModelState.IsValid)
            //{
            //    string wwwRootPath = _webHostEnvironment.WebRootPath;
            //    if (file != null)
            //    {
            //        //Guid = globally unique identifier & here we are saying we want fileName to be assigned a globally unique identifier
            //        //so the files being imported using the upsert method will always have a unique name.
            //        string fileName = Guid.NewGuid().ToString();

            //        //Path.Combine will combine 2 strings into a path. we are combining the 'wwwroot' folder path w/ the 'images\products' folder path
            //        var uploads = Path.Combine(wwwRootPath, @"images\products");

            //        //Path.GetExtension returns extensions of the specified path string. 
            //        //We want to get the extensions of the file being uploaded using the upsert method
            //        var extension = Path.GetExtension(file.FileName);

            //        using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create)
            //        {
            //             file.CopyTo(fileStreams);
                
            //        }
            //    }
            //}            
        





    
















