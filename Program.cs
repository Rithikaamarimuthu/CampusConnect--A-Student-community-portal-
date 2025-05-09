using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CommunityContext>(opt => opt.UseInMemoryDatabase("CommunityDB"));
builder.Services.AddCors(); // Enable CORS for frontend calls

var app = builder.Build();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

// API: Get all posts
app.MapGet("/api/posts", async (CommunityContext db) =>
    await db.Posts.OrderByDescending(p => p.Timestamp).ToListAsync()
);

// API: Add a new post
app.MapPost("/api/posts", async (Post post, CommunityContext db) =>
{
    post.Timestamp = DateTime.UtcNow;
    db.Posts.Add(post);
    await db.SaveChangesAsync();
    return Results.Created($"/api/posts/{post.Id}", post);
});

// Serve the frontend HTML
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>CampusConnect - Student Community Portal</title>
    <style>
        body { font-family: Arial; margin: 40px; }
        h1 { color: #007ACC; }
        .post { border: 1px solid #ccc; padding: 10px; margin: 10px 0; border-radius: 8px; }
        .timestamp { color: gray; font-size: 0.8em; }
    </style>
</head>
<body>
    <h1>CampusConnect – Student Community Portal</h1>
    <form id='postForm'>
        <input type='text' id='author' placeholder='Your Name' required /><br/><br/>
        <textarea id='content' placeholder='Share something...' required></textarea><br/><br/>
        <button type='submit'>Post</button>
    </form>
    <hr/>
    <div id='posts'></div>

    <script>
        const form = document.getElementById('postForm');
        const postsDiv = document.getElementById('posts');

        async function loadPosts() {
            const res = await fetch('/api/posts');
            const posts = await res.json();
            postsDiv.innerHTML = posts.map(p => `
                <div class='post'>
                    <strong>${p.author}</strong><br/>
                    ${p.content}<br/>
                    <div class='timestamp'>${new Date(p.timestamp).toLocaleString()}</div>
                </div>
            `).join('');
        }

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const author = document.getElementById('author').value;
            const content = document.getElementById('content').value;

            await fetch('/api/posts', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ author, content })
            });

            form.reset();
            loadPosts();
        });

        loadPosts();
    </script>
</body>
</html>
");
});

app.Run();

// --- Data Models and DB Context ---
public class Post
{
    public int Id { get; set; }

    [Required]
    public string Author { get; set; }

    [Required]
    public string Content { get; set; }

    public DateTime Timestamp { get; set; }
}

public class CommunityContext : DbContext
{
    public CommunityContext(DbContextOptions<CommunityContext> options) : base(options) { }

    public DbSet<Post> Posts { get; set; }
}
