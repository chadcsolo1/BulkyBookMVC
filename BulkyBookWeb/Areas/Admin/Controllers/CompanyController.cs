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

    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            
        }

        public IActionResult Index()
        {
            

            return View();
        }

        //GET
        public IActionResult Upsert(int? id)
        {
            Company company = new();
            


            if (id == null || id == 0)
            {
                

                return View(company);
            }
            else
            {
                
                company = _unitOfWork.Company.GetFirstOrDefault(i => i.Id == id);
                return View(company);
            }

            
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj)
        {
            if (ModelState.IsValid)
            {
                

                if(obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                } else
                {
                    _unitOfWork.Company.Update(obj);
                }

                    
                    _unitOfWork.Save();
                    TempData["success"] = "Product Created Successfully";
                    return RedirectToAction("Index");

            }
               return View(obj); 

        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(int id)
        {
            var companyList = _unitOfWork.Company.GetAll();
            return Json(new {data =  companyList});
        }

        //POST
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            //Get Company OBJ based on ID then check if null & if null return Json error
            var obj = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);
            if (obj == null)
            {
                return Json(new {success = false,message = "Error while deleting"});
            }

            //Remove company save to DB and return json success message
            _unitOfWork.Company.Delete(obj);
            _unitOfWork.Save();
            return Json(new {success = true,message = "Delete Successful"});
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
        





    
















