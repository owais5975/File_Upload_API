using FileUpload_Web_API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace FileUpload_Web_API.Controllers
{
    public class UploadController : ApiController
    {
        class Global
        {
            public static bool  Check;

        }
        public Task<HttpResponseMessage> Post()
        {
            HttpResponseMessage response = null;
            // Check if the request contains multipart/form-data
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            //Get the path of folder where we want to upload all files.
            string rootPath = HttpContext.Current.Server.MapPath("~/FilesUploaded");

            var provider = new MultipartFileStreamProvider(rootPath);

            // Read the form data.
            //If any error(Cancelled or any fault) occurred during file read , return internal server error
            var task = Request.Content.ReadAsMultipartAsync(provider).
                ContinueWith<HttpResponseMessage>(t =>
                {
                    if (t.IsCanceled || t.IsFaulted)
                    {
                        Request.CreateErrorResponse(HttpStatusCode.InternalServerError, t.Exception);
                    }
                    foreach (MultipartFileData dataitem in provider.FileData)
                    {
                        try
                        {
                            var extensions = new List<string>(){".jpg",".jpeg",".png",".docx", ".pdf" };
                            //Replace / from file name
                            string file_name = dataitem.Headers.ContentDisposition.FileName.Replace("\"", "");
                            //Checking file extension validity
                            var file_extension = Path.GetExtension(file_name);
                            for (int i = 0; i < extensions.Count(); i++)
                            {
                                Global.Check = (file_extension.ToLower() == extensions[i]) ? true : false;
                                if(Global.Check == true)
                                {
                                    break;
                                }
                            }
                                if (!Global.Check)
                                {
                                     response = Request.CreateErrorResponse(HttpStatusCode.BadRequest, "File Format is InValid");
                                }
                                else
                                {
                                    Upload_tbl upload = new Upload_tbl();

                                    string newFileName = string.Empty;

                                    using (DBConnection db = new DBConnection())
                                    {
                                        int? DB_Id = db.Upload_tbl.Max(u => (int?)u.Id) + 1;
                                        int? id = (DB_Id == null) ? 0 : DB_Id;

                                        //Create New Unique file name 
                                        newFileName = id + "_" + file_name;

                                        upload.Image_path = newFileName;
                                        db.Upload_tbl.Add(upload);
                                        db.SaveChanges();
                                    }
                                    //Move file from current location to target folder.
                                    File.Move(dataitem.LocalFileName, Path.Combine(rootPath, newFileName));
                                     response = Request.CreateResponse(HttpStatusCode.Created, "Successfully Uploaded: " + newFileName.ToString());
                                }
                        }
                        catch (Exception exception)
                        {
                            response = Request.CreateErrorResponse(HttpStatusCode.BadRequest, exception);
                        }
                    }
                    return response;
                });
            return task;
        }

    }
}
