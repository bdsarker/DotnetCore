using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController: BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context, ITokenService tokenService){
            _tokenService=tokenService;
            _context = context;
        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto){
            
            if(await userExists(registerDto.UserName))
                return BadRequest("User Name is taken");

            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName=registerDto.UserName.ToLower(),
                PasswordHash= hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt=hmac.Key
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserDto{
                UserName=user.UserName,
                Token=_tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto){
            
            var user=await _context.Users.SingleOrDefaultAsync(x => x.UserName.Equals(loginDto.UserName));
            if(user==null)
            return Unauthorized("Invalid User name");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computeHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for(int i=0;i<computeHash.Length;i++){
                if(computeHash[i] != user.PasswordHash[i])
                return Unauthorized("Invalid Password");
            }
             return new UserDto{
                UserName=user.UserName,
                Token=_tokenService.CreateToken(user)
            };

        }

        private async Task<bool> userExists(String userName){
            return await _context.Users.AnyAsync(x=>x.UserName.Equals(userName.ToLower()));
        }
    }

}