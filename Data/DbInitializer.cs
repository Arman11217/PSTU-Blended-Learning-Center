using Microsoft.AspNetCore.Identity;
using PBLC.Web.Models.Entities;
using PBLC.Web.Models.Enums;

namespace PBLC.Web.Data
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Check if users already exist - if they do, don't seed
            if (await userManager.FindByNameAsync("admin") != null)
                return; // Database has been seeded

            // Check if data already exists
            if (context.Departments.Any() || context.Faculties.Any())
                return; // Database has been seeded

            // Create Faculties
            var faculties = new List<Faculty>
            {
                new Faculty
                {
                    Name = "Faculty of Engineering",
                    Description = "Engineering disciplines including CSE, EEE, CE",
                    CreatedAt = DateTime.Now
                },
                new Faculty
                {
                    Name = "Faculty of Science",
                    Description = "Science and technology programs",
                    CreatedAt = DateTime.Now
                },
                new Faculty
                {
                    Name = "Faculty of Business Studies",
                    Description = "Business administration and management",
                    CreatedAt = DateTime.Now
                }
            };

            context.Faculties.AddRange(faculties);
            await context.SaveChangesAsync();

            // Create Departments
            var departments = new List<Department>
            {
                new Department
                {
                    Name = "Computer Science & Engineering",
                    Description = "Department of CSE",
                    FacultyId = faculties[0].Id,
                    CreatedAt = DateTime.Now
                },
                new Department
                {
                    Name = "Electrical & Electronic Engineering",
                    Description = "Department of EEE",
                    FacultyId = faculties[0].Id,
                    CreatedAt = DateTime.Now
                },
                new Department
                {
                    Name = "Civil Engineering",
                    Description = "Department of CE",
                    FacultyId = faculties[0].Id,
                    CreatedAt = DateTime.Now
                }
            };

            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();

            // Create Admin User
            var admin = new ApplicationUser
            {
                FullName = "System Administrator",
                UserName = "admin",  // Username for login
                Email = "admin@pstu.ac.bd",
                Role = UserRole.Admin,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123");

            // Create Teacher Users
            var teacher1 = new ApplicationUser
            {
                FullName = "Mr. Teacher",
                UserName = "teacher",  // Username for login
                Email = "teacher@pstu.ac.bd",
                Role = UserRole.Teacher,
                FacultyId = faculties[0].Id,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(teacher1, "Teacher@123");

            var teacher2 = new ApplicationUser
            {
                FullName = "Dr. Karim",
                UserName = "karim",  // Username for login
                Email = "karim@pstu.ac.bd",
                Role = UserRole.Teacher,
                FacultyId = faculties[0].Id,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(teacher2, "Teacher@123");

            // Create Student Users
            var student1 = new ApplicationUser
            {
                FullName = "Mr. Student",
                UserName = "student",  // Username for login
                Email = "student@pstu.ac.bd",
                Role = UserRole.Student,
                FacultyId = faculties[0].Id,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(student1, "Student@123");

            var student2 = new ApplicationUser
            {
                FullName = "Fatima Khan",
                UserName = "fatima",  // Username for login
                Email = "fatima@pstu.ac.bd",
                Role = UserRole.Student,
                FacultyId = faculties[0].Id,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(student2, "Student@123");

            var student3 = new ApplicationUser
            {
                FullName = "Sabbir Hossain",
                UserName = "sabbir",  // Username for login
                Email = "sabbir@pstu.ac.bd",
                Role = UserRole.Student,
                FacultyId = faculties[0].Id,
                CreatedAt = DateTime.Now,
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(student3, "Student@123");

            // Create Courses
            var courses = new List<Course>
            {
                new Course
                {
                    Name = "Data Structures and Algorithms",
                    Code = "CSE-201",
                    Description = "Learn about fundamental data structures and algorithms",
                    DepartmentId = departments[0].Id,
                    TeacherId = teacher1.Id,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new Course
                {
                    Name = "Database Management Systems",
                    Code = "CSE-301",
                    Description = "Comprehensive course on DBMS concepts and SQL",
                    DepartmentId = departments[0].Id,
                    TeacherId = teacher1.Id,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new Course
                {
                    Name = "Object Oriented Programming",
                    Code = "CSE-101",
                    Description = "Learn OOP concepts with C# and .NET",
                    DepartmentId = departments[0].Id,
                    TeacherId = teacher2.Id,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                }
            };

            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            // Enroll students in courses
            student1.EnrolledCourses = new List<Course> { courses[0], courses[1] };
            student2.EnrolledCourses = new List<Course> { courses[0], courses[2] };
            student3.EnrolledCourses = new List<Course> { courses[1], courses[2] };
            await context.SaveChangesAsync();

            // Create Lectures
            var lectures = new List<Lecture>
            {
                new Lecture
                {
                    Title = "Introduction to Arrays",
                    Description = "Basic concepts of arrays and their operations",
                    CourseId = courses[0].Id,
                    CreatedAt = DateTime.Now,
                    OrderNumber = 1
                },
                new Lecture
                {
                    Title = "Linked Lists",
                    Description = "Understanding linked list data structure",
                    CourseId = courses[0].Id,
                    CreatedAt = DateTime.Now,
                    OrderNumber = 2
                },
                new Lecture
                {
                    Title = "Introduction to Databases",
                    Description = "Overview of database systems",
                    CourseId = courses[1].Id,
                    CreatedAt = DateTime.Now,
                    OrderNumber = 1
                }
            };

            context.Lectures.AddRange(lectures);
            await context.SaveChangesAsync();

            // Create Assignments
            var assignments = new List<Assignment>
            {
                new Assignment
                {
                    Title = "Array Implementation Assignment",
                    Description = "Implement various array operations in C#",
                    CourseId = courses[0].Id,
                    TeacherId = teacher1.Id,
                    DueDate = DateTime.Now.AddDays(7),
                    MaxMarks = 10,
                    CreatedAt = DateTime.Now
                },
                new Assignment
                {
                    Title = "SQL Query Assignment",
                    Description = "Write SQL queries for given problems",
                    CourseId = courses[1].Id,
                    TeacherId = teacher1.Id,
                    DueDate = DateTime.Now.AddDays(10),
                    MaxMarks = 15,
                    CreatedAt = DateTime.Now
                },
                new Assignment
                {
                    Title = "OOP Project",
                    Description = "Create a simple OOP project in C#",
                    CourseId = courses[2].Id,
                    TeacherId = teacher2.Id,
                    DueDate = DateTime.Now.AddDays(14),
                    MaxMarks = 20,
                    CreatedAt = DateTime.Now
                }
            };

            context.Assignments.AddRange(assignments);
            await context.SaveChangesAsync();

            // Create some assignment submissions
            var submissions = new List<AssignmentSubmission>
            {
                new AssignmentSubmission
                {
                    AssignmentId = assignments[0].Id,
                    StudentId = student1.Id,
                    Comments = "Here is my solution",
                    SubmittedAt = DateTime.Now.AddDays(-1),
                    ObtainedMarks = 8,
                    Feedback = "Good work!",
                    EvaluatedAt = DateTime.Now
                },
                new AssignmentSubmission
                {
                    AssignmentId = assignments[0].Id,
                    StudentId = student2.Id,
                    Comments = "My array implementation",
                    SubmittedAt = DateTime.Now.AddDays(-2),
                    ObtainedMarks = 9,
                    Feedback = "Excellent!",
                    EvaluatedAt = DateTime.Now.AddDays(-1)
                }
            };

            context.AssignmentSubmissions.AddRange(submissions);
            await context.SaveChangesAsync();

            // Create Questions
            var questions = new List<Question>
            {
                new Question
                {
                    Title = "Difference between Array and LinkedList?",
                    Content = "Can someone explain the main differences between arrays and linked lists?",
                    CourseId = courses[0].Id,
                    StudentId = student1.Id,
                    CreatedAt = DateTime.Now.AddDays(-2),
                    IsResolved = false
                },
                new Question
                {
                    Title = "SQL JOIN confusion",
                    Content = "I'm confused about INNER JOIN vs LEFT JOIN. Can anyone clarify?",
                    CourseId = courses[1].Id,
                    StudentId = student3.Id,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsResolved = false
                }
            };

            context.Questions.AddRange(questions);
            await context.SaveChangesAsync();

            // Create Answers
            var answers = new List<Answer>
            {
                new Answer
                {
                    Content = "Arrays have fixed size and contiguous memory, while linked lists are dynamic and use pointers.",
                    QuestionId = questions[0].Id,
                    UserId = teacher1.Id,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsAccepted = true
                },
                new Answer
                {
                    Content = "INNER JOIN returns only matching rows, while LEFT JOIN returns all rows from left table.",
                    QuestionId = questions[1].Id,
                    UserId = teacher1.Id,
                    CreatedAt = DateTime.Now.AddHours(-2),
                    IsAccepted = false
                }
            };

            context.Answers.AddRange(answers);
            await context.SaveChangesAsync();
        }
    }
}
