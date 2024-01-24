using Fuen31Site.Models;
using Fuen31Site.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Eventing.Reader;

namespace Fuen31Site.Controllers
{
    public class ApiController : Controller
    {
        private readonly MyDBContext _dbContext;
        private readonly IWebHostEnvironment _host; //檔案路徑的全域變數
        public ApiController(MyDBContext dbContext, IWebHostEnvironment host)
        {
            _dbContext = dbContext;
            _host = host;
        }

        
        public IActionResult Index()
        {
            //範例:Content (string content, string contentType,System.Text.Encoding contentEncoding); 
            //return Content("Hello World"); //純英文 不用寫型別
            //return Content("<h2<Hello World</h2>","text/html"); //顯示<h2>標籤功能的話, 用text/html

            System.Threading.Thread.Sleep(3000); //3秒
            return Content("<h2>Hello World ,你好</h2>", "text/html", System.Text.Encoding.UTF8); //有中文 要寫System.Text.Encoding.UTF8
        }

       
        public IActionResult Cities()
        {
            var cities = _dbContext.Addresses.Select(a => a.City).Distinct();
            return Json(cities);
        }
        public IActionResult Districts(string city) {
            var districts = _dbContext.Addresses.Where(a => a.City == city).Select(a => a.SiteId).Distinct();
            return Json(districts);
        }

        public IActionResult Avatar(int id=1) {
            Member? member = _dbContext.Members.Find(id);
            if (member != null)
            {
                byte[] img = member.FileData;
                return File(img, "image/jpeg"); //回傳File() ,是img , image/jpeg 型別
            }

            return NotFound(); //沒找到就回傳NotFound()
        }


        //接收-- (方法1)
        //public IActionResult Register(string name, int age = 26)
        //{
        //    if (string.IsNullOrEmpty(name))
        //    {
        //        name = "guest";
        //    }

        //    return Content($"Hello {name}, You are {age} years old");
        //}


        //接收-- (方法2) ,多個傳入參數時 --使用帶入class DTO 
        //public IActionResult Register(UserDTO _userDto)
        //{
        //    if (string.IsNullOrEmpty(_userDto.Name))
        //    {
        //        _userDto.Name = "guest";
        //    }
        //    //設定檔案名字
        //    string fileName = "empty.jpg";
        //    if(_userDto.Avatar != null)
        //    {
        //        fileName = _userDto.Avatar.FileName;
        //    }
        //    //取得檔案上傳的實際路徑
        //    string uploadPath = Path.Combine(_host.WebRootPath,"uploads",fileName); //WebRootPath是取得wwwroot的實際路徑 ,uploads是資料夾名字, fileName是檔案名字
        //    //檔案上傳  FileStream()
        //    using (var fileStream = new FileStream(uploadPath, FileMode.Create))
        //    {
        //        _userDto.Avatar?.CopyTo(fileStream);
        //    }


        //    //return Content($"Hello:{_userDto.Name}, 您 {_userDto.Age} 幾歲, 您的信箱是:{_userDto.Email}"); //顯示回傳接收到的資訊 --(1)
        //    return Content($"檔名:{_userDto.Avatar?.FileName}-檔案大小:{_userDto.Avatar?.Length}-檔案類型:{_userDto.Avatar?.ContentType}"); //顯示回傳看看接收到的圖片資訊--(2) ,名字-檔案大小-檔案類型

        //}



        [HttpPost]
        //接收-- (方法3) 多一個將檔案轉成二進制 ,並存入資料庫
        public IActionResult Register(Member member, IFormFile Avatar)
        {
            string fileName = "empty.jpg";
            if (Avatar != null)
            {
                fileName = Avatar.FileName;
            }

            //取得檔案上傳的實際路徑
            string uploadPath = Path.Combine(_host.WebRootPath, "uploads", fileName);
            //檔案上傳 FileStream()
            using (var fileStream = new FileStream(uploadPath, FileMode.Create))
            {
                Avatar?.CopyTo(fileStream);
            }

            //轉成二進位 MemoryStream()
            byte[]? imgByte = null;
            using (var memoryStream = new MemoryStream())
            {
                Avatar?.CopyTo(memoryStream);
                imgByte = memoryStream.ToArray();
            }

            member.FileName = fileName;
            member.FileData = imgByte;

            //新增
            _dbContext.Members.Add(member);
            _dbContext.SaveChanges();

            return Content("新增成功");

            // return Content($"Hello {_user.Name}, {_user.Age}歲了,電子郵件是{_user.Email}");
            //return Content($"{_user.Avatar?.FileName}-{_user.Avatar?.Length}-{_user.Avatar?.ContentType}");
        }



        //接收-- 老師版(方法2) ,多個傳入參數時 --使用帶入class DTO 
        //public IActionResult Register(UserDTO _user)
        //{
        //    if (string.IsNullOrEmpty(_user.Name))
        //    {
        //        _user.Name = "Guest";
        //    }
        //    //string uploadPath = @"C:\Users\ispan\Documents\workspace\Fuen31Site\wwwroot\uploads\fileName.jpg";

        //    //todo 檔案存在的處理
        //    //todo 限制上傳的檔案類型
        //    //todo 限制上傳的檔案大小

        //    string fileName = "empty.jpg";
        //    if(_user.Avatar != null)
        //    {
        //        fileName = _user.Avatar.FileName;
        //    }
        //    //取得檔案上傳的實際路徑
        //    string uploadPath = Path.Combine(_host.WebRootPath, "uploads", fileName);
        //    //檔案上傳
        //    using (var fileStream = new FileStream(uploadPath, FileMode.Create))
        //    {
        //        _user.Avatar?.CopyTo(fileStream);
        //    }

        //    //轉成二進位
        //    byte[]? imgByte = null;
        //    using(var memoryStream = new MemoryStream())
        //    {
        //        _user.Avatar?.CopyTo(memoryStream);
        //        imgByte = memoryStream.ToArray();
        //    }


        //    // return Content($"Hello {_user.Name}, {_user.Age}歲了,電子郵件是{_user.Email}");
        //  return Content($"{_user.Avatar?.FileName}-{_user.Avatar?.Length}-{_user.Avatar?.ContentType}");
        //}



        [HttpPost]
        public IActionResult Spots([FromBody]SearchDTO _search)//因為要接收JSON格式的資料 ,所以用[FromBody]來接收 
        {
            
            //根據分類編號讀取景點資料--(搜尋)
            var spots = _search.categoryId == 0 ? _dbContext.SpotImagesSpots : _dbContext.SpotImagesSpots.Where(s => s.CategoryId == _search.categoryId);

            //根據關鍵字搜尋--(搜尋)
            if (!string.IsNullOrEmpty(_search.keyword))
            {
               spots = spots.Where(s => s.SpotTitle.Contains(_search.keyword) || s.SpotDescription.Contains(_search.keyword));
            }

            //排序
            switch (_search.sortBy)
            {
                case "spotTitle":
                    spots = _search.sortType == "asc" ? spots.OrderBy(s=>s.SpotTitle) : spots.OrderByDescending
                        (s=>s.SpotTitle);                        
                    break;
                case "categoryId":
                    spots = _search.sortType == "asc" ? spots.OrderBy(s => s.CategoryId) : spots.OrderByDescending
                       (s => s.CategoryId);
                    break;
                default:
                    spots = _search.sortType == "asc" ? spots.OrderBy(s => s.SpotId) : spots.OrderByDescending
                       (s => s.SpotId);
                    break;
            }

            //分頁
            int TotalCount = spots.Count(); //搜尋出來的筆資料總共有幾筆
            int pageSize = _search.pageSize ?? 9; //每頁多少筆資料 沒給就(預設9筆)
            int TotalPages =(int)Math.Ceiling((decimal)TotalCount / pageSize); //計算出總共有幾頁
            int page = _search.Page ?? 1 ; //目前第幾頁 ,沒給就(預設第一頁)

            //取出分頁資料
            spots = spots.Skip((page - 1) * pageSize).Take(pageSize);

            //設計要回傳的資料格式
            SpotsPagingDTO spotsPaging = new SpotsPagingDTO();
            spotsPaging.TotalPages = TotalPages;
            spotsPaging.SpotsReslut = spots.ToList();

            //return Json(spots);
            return Json(spotsPaging);
        }




        public IActionResult CheckAccount(string name)
        {
            Member? member = _dbContext.Members.Where(x => x.Name == name).FirstOrDefault();

            if (member != null)
            {
                return Content($"{name}已經註冊過了");
            }
            return Content($"{name}您可以註冊");

        }
    }
}
