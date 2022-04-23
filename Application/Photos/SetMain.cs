using AForge.Imaging;
using Application.Core;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Application.Photos
{
    public class SetMain
    {
        public class Command : IRequest<Result<Unit>>
        {
            public string Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<Unit>>
        {
            private readonly DataContext _context;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IUserAccessor userAccessor)
            {
                _context = context;
                _userAccessor = userAccessor;
            }

            public async Task<Result<Unit>> Handle(Command request,  CancellationToken cancellationToken)
            {
                HttpClient req = new HttpClient();
                

                var user = await _context.Users.Include(p => p.Photos)
                    .FirstOrDefaultAsync(x => x.UserName == _userAccessor.GetUsername());

                if (user == null) return null;

                var photo = user.Photos.FirstOrDefault(x => x.Id == request.Id);

                if (photo == null) return null;

                var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

                if (currentMain != null)    // If user has a main photo already
                 {
                    // Use URL from the current main photo object to GET the actual image file
                    var currentMainReq = await req.GetAsync(currentMain.Url, cancellationToken);
                    var currentMainImg = await currentMainReq.Content.ReadAsStreamAsync();
                    var currentMainBitmap = new Bitmap(currentMainImg);

                    // Do the same for the selected photo
                    var photoReq = await req.GetAsync(photo.Url, cancellationToken);
                    var photoImg = await photoReq.Content.ReadAsStreamAsync();
                    var photoBitmap = new Bitmap(photoImg);



                    // create template matching algorithm's instance
                    // use zero similarity to make sure algorithm will provide anything
                    ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0);
                    // compare two images
                    TemplateMatch[] matchings = tm.ProcessImage(photoBitmap, currentMainBitmap);
                    // check similarity level
                    Console.WriteLine(matchings[0].Similarity);
                    if (matchings[0].Similarity > 0.95f)
                    {
                        return Result<Unit>.Failure("The photo is too similar to current. \n - Similarity: " + matchings[0].Similarity);
                    }
                    currentMain.IsMain = false;
                }
                    

                photo.IsMain = true;

                var success = await _context.SaveChangesAsync() > 0;

                if (success) return Result<Unit>.Success(Unit.Value);

                return Result<Unit>.Failure("Problem setting main photo");
            }
        }
    }
}
