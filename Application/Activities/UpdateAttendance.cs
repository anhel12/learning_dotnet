using Application.Core;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Activities
{
    public class UpdateAttendance
    {

        public class Command : IRequest<Result<Unit>>
        {
            public Guid Id { get; set; }
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

            public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Find the activity in db that was specified in the URL
                var activity = await _context.Activities
                    .Include(a => a.Attendees).ThenInclude(u => u.AppUser)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                // If activity not found, stop running code
                if (activity == null) return null;      

                // Find currently active user
                var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.UserName == _userAccessor.GetUsername());

                // If user not found, stop running code
                if (user == null) return null;         

                // Go through attendee list of the activity to get the name of the host
                var hostUsername = activity.Attendees.FirstOrDefault(x => x.IsHost)?.AppUser?.UserName;

                // Go through attendee list of the activity to see if the currently active user is there
                var attendance = activity.Attendees.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

                // The user making the request is also the host = cancel the activity
                if (attendance != null && hostUsername == user.UserName)
                    activity.IsCancelled = !activity.IsCancelled;

                // An attendee is making the request = remove from attending
                if (attendance != null && hostUsername != user.UserName)
                    activity.Attendees.Remove(attendance);

                // If the requester is not currently attending, create new object and add to list
                if(attendance == null)
                {
                    attendance = new ActivityAttendee
                    {
                        AppUser = user,
                        Activity = activity,
                        IsHost = false
                    };
                    activity.Attendees.Add(attendance);
                }

                // Save context changes to DB and return true if more than zero changes were saved.
                bool result = await _context.SaveChangesAsync() > 0;
                return result ? Result<Unit>.Success(Unit.Value) : Result<Unit>.Failure("Problem updating attendance");
            }
        }
    }
}
