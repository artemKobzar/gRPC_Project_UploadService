using Azure.Core;
using Grpc.Core;
using GrpcService;
using GrpcService.Data;
using GrpcService.Models;
using Microsoft.EntityFrameworkCore;

namespace GrpcService.Services
{
    public class MovieService : MovieCrud.MovieCrudBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<GreeterService> _logger;
        public MovieService(ILogger<GreeterService> logger, AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public override async Task<CreateMovieResponse> CreateMovie(CreateMovieRequest request, ServerCallContext context)
        {
            if (request.Title == string.Empty || request.Description == string.Empty
                || request.ReleaseDate == string.Empty)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "You must supply a valid object"));

            var movie = new Movie
            {
                Title = request.Title,
                Description = request.Description,
                DateRelease = request.ReleaseDate
            };

            await _dbContext.AddAsync(movie);
            await _dbContext.SaveChangesAsync();

            return await Task.FromResult(new CreateMovieResponse
            {
                Id = movie.Id
            });
        }

        public override async Task<GetAllMovieResponse> GetAllMovie (GetAllMovieRequest request, ServerCallContext context)
        {
            var response = new GetAllMovieResponse();
            var movies = await _dbContext.Movies.ToListAsync();
            foreach(var movie in movies)
            {
                response.Movies.Add(new GetListMovieResponse
                {
                    Title = movie.Title,
                    ReleaseDate = movie.DateRelease
                });
            }
            return await Task.FromResult(response);
        }

        public override async Task<GetMovieResponse> GetMovie(GetMovieRequest request, ServerCallContext context)
        {
            if (request.Id <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "resource index must be greater than 0"));

            var movie = await _dbContext.Movies.FirstOrDefaultAsync(movie => movie.Id == request.Id);

            if (movie != null)
            {
                return await Task.FromResult(new GetMovieResponse
                {
                    Title = movie.Title,
                    Description = movie.Description,
                    ReleaseDate = movie.DateRelease,
                });
            }

            throw new RpcException(new Status(StatusCode.NotFound, $"No movie with id {request.Id}"));
        }
        public override async Task<UpdateMovieResponse> UpdateMovie(UpdateMovieRequest request, ServerCallContext context)
        {
            if (request.Id <= 0 || request.Title == string.Empty || request.Description == string.Empty
                || request.ReleaseDate == string.Empty)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "You must supply a valid object"));

            var movie = await _dbContext.Movies.FirstOrDefaultAsync(movie => movie.Id == request.Id);

            if (movie == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"No Task with Id {request.Id}"));

            movie.Title = request.Title;
            movie.Description = request.Description;
            movie.DateRelease = request.ReleaseDate;

            await _dbContext.SaveChangesAsync();

            return await Task.FromResult(new UpdateMovieResponse
            {
                Id = movie.Id
            });
        }
        public override async Task<DeleteMovieResponse> DeleteMovie(DeleteMovieRequest request, ServerCallContext context)
        {
            if (request.Id <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "resource index must be greater than 0"));

            var movie = await _dbContext.Movies.FirstOrDefaultAsync(movie => movie.Id == request.Id);

            if (movie == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"No Task with Id {request.Id}"));

            _dbContext.Remove(movie);

            await _dbContext.SaveChangesAsync();

            return await Task.FromResult(new DeleteMovieResponse
            {
                Id = movie.Id
            });
        }

        public override async Task<MultyGetMovieResponse> GetAllMovieStream(IAsyncStreamReader<GetMovieRequest> requestStream, ServerCallContext context)
        {
            var response = new MultyGetMovieResponse();
            await foreach(var request in  requestStream.ReadAllAsync())
            {
                var movie = await _dbContext.Movies.FirstOrDefaultAsync(mov => mov.Id == request.Id);
                if (movie == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, $"No movie with id {request.Id}"));
                }
                else
                {
                    response.Movie.Add(new GetMovieResponse
                    {
                        Title = movie.Title,
                        Description = movie.Description,
                        ReleaseDate = movie.DateRelease
                    });
                }                
            }
            return response;
        }
        //public override async Task CreateMovieStream(CreateMovieRequest request, IAsyncStreamReader<CreateMovieResponseStream> response, ServerCallContext context)
        //{
        //    if (request.Title == string.Empty || request.Description == string.Empty
        //        || request.ReleaseDate == string.Empty)
        //        throw new RpcException(new Status(StatusCode.InvalidArgument, "You must supply a valid object"));

        //    var movie = new Movie
        //    {
        //        Title = request.Title,
        //        Description = request.Description,
        //        DateRelease = request.ReleaseDate
        //    };

        //    await _dbContext.AddAsync(movie);
        //    await _dbContext.SaveChangesAsync();

        //    return await Task.FromResult(new CreateMovieResponseStream
        //    {
        //        Movie = movie.Id
        //    });
        //}
    }
}
