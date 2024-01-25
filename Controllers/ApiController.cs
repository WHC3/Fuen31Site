using Fuen31Site.Models;
using Fuen31Site.Models.DTO;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;

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
        public IActionResult Districts(string city)
        {
            var districts = _dbContext.Addresses.Where(a => a.City == city).Select(a => a.SiteId).Distinct();
            return Json(districts);
        }

        public IActionResult Avatar(int id = 1)
        {
            Member? member = _dbContext.Members.Find(id);
            if (member != null)
            {
                byte[] img = member.FileData;
                return File(img, "image/jpeg"); //回傳File() ,是img , image/jpeg 型別
            }

            return NotFound(); //沒找到就回傳NotFound()
        }



        //接收-- (方法1)  只有兩個傳入參數
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
        //    設定檔案名字
        //    string fileName = "empty.jpg";
        //    if (_userDto.Avatar != null)
        //    {
        //        fileName = _userDto.Avatar.FileName;
        //    }

        //    //todo 檔案存在的處理
        //    //todo 限制上傳的檔案類型
        //    //todo 限制上傳的檔案大小

        //    取得檔案上傳的實際路徑
        //    string uploadPath = Path.Combine(_host.WebRootPath, "uploads", fileName); //WebRootPath是取得wwwroot的實際路徑 ,uploads是資料夾名字, fileName是檔案名字
        //    檔案上傳 FileStream()
        //    using (var fileStream = new FileStream(uploadPath, FileMode.Create))
        //    {
        //        _userDto.Avatar?.CopyTo(fileStream);
        //    }

        //    轉成二進位
        //    byte[]? imgByte = null;
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        _userDto.Avatar?.CopyTo(memoryStream);
        //        imgByte = memoryStream.ToArray();
        //    }

        //    return Content($"Hello:{_userDto.Name}, 您 {_userDto.Age} 幾歲, 您的信箱是:{_userDto.Email}"); //顯示回傳接收到的資訊 --(1)
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

        }



        [HttpPost]
        public IActionResult Spots([FromBody] SearchDTO _search) //因為要接收JSON格式的資料 ,所以用[FromBody]來接收 
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
                case "spotTitle": //根據景點名稱排序
                    spots = _search.sortType == "asc" ? spots.OrderBy(s => s.SpotTitle) : spots.OrderByDescending
                        (s => s.SpotTitle);
                    break;
                case "categoryId": //根據分類編號排序
                    spots = _search.sortType == "asc" ? spots.OrderBy(s => s.CategoryId) : spots.OrderByDescending
                       (s => s.CategoryId);
                    break;
                default:  //預設狀況
                    spots = _search.sortType == "asc" ? spots.OrderBy(s => s.SpotId) : spots.OrderByDescending
                       (s => s.SpotId);
                    break;
            }

            //分頁
            int TotalCount = spots.Count(); //搜尋出來的筆資料總共有幾筆
            int pageSize = _search.pageSize ?? 9; //每頁多少筆資料 沒給就(預設9筆)
            int TotalPages = (int)Math.Ceiling((decimal)TotalCount / pageSize); //計算出總共有幾頁
            int page = _search.Page ?? 1; //目前第幾頁 ,沒給就(預設第一頁)

            //取出分頁資料
            spots = spots.Skip((page - 1) * pageSize).Take(pageSize);

            //設計要回傳的資料格式
            SpotsPagingDTO spotsPaging = new SpotsPagingDTO();
            spotsPaging.TotalPages = TotalPages;
            spotsPaging.SpotsReslut = spots.ToList();


            //return Json(spots);
            return Json(spotsPaging);

        }

        public IActionResult SpotsTitle(string keyword)
        {
            var spots = _dbContext.Spots.Where(s => s.SpotTitle.Contains(keyword)) //搜尋
                                        .Select(s => s.SpotTitle) //只取景點名稱
                                        .Take(8);   //只取8筆
            return Json(spots);  //回傳Json格式的(8筆SpotTitle資料)

        }



        public IActionResult CheckAccount(string name) //作業2
        {
            Member? member = _dbContext.Members.Where(x => x.Name == name).FirstOrDefault();

            if (member != null)
            {
                return Content($"{name}已經註冊過了");
            }

            return Content($"{name}您可以註冊");
        }



        [HttpPost]
        public IActionResult Homework3(Member member, IFormFile Avatar) //作業3
        {
            string fileName = "empty.jpg";
            if (Avatar != null)
            {
                fileName = Avatar.FileName;
            }

            //取得檔案上傳的實際路徑
            string uploadPath = Path.Combine(_host.WebRootPath, "uploads", fileName); //WebRootPath是取得在wwwroot的實際路徑 ,uploads是資料夾名字, fileName是檔案名字
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

            //判斷密碼與確認密碼是否相同
           if(member.Password != member.Salt)
            {
                return Content("密碼與確認密碼不同");               
            }

            //密碼加鹽          
            // 設定 PBKDF2 參數
            int iterations = 10000;
            int numBytesRequested = 32;
            //產生鹽
            byte[] salt = GenerateRandomSalt();
            // 使用 PBKDF2 加密
            byte[] hashedPassword = KeyDerivation.Pbkdf2(member.Password, salt, KeyDerivationPrf.HMACSHA512, iterations, numBytesRequested);

            // salt 和 Password 可以被儲存為位元組陣列或轉換成十六進位字串
            member.Salt = Convert.ToBase64String(salt);
            member.Password = Convert.ToBase64String(hashedPassword);


            //新增進資料庫
            _dbContext.Members.Add(member);
            _dbContext.SaveChanges();

            return Content($"新增成功,姓名:{member.Name},信箱:{member.Email},密碼:{member.Password},年紀:{member.Age},");

        }


        // 產生鹽 //
        private static byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[16]; // 16位元組的加鹽
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }


    }
}
