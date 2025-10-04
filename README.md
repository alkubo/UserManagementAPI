# 🧑‍💻 TechHive Solutions – User Management API

A **.NET 9 Minimal API** project built for TechHive Solutions to manage users.  
This API provides **CRUD operations**, **validation**, and **middleware** for logging, authentication, and error handling.

---

## ✨ Features
- ✅ CRUD endpoints (`GET`, `POST`, `PUT`, `DELETE`) for managing users  
- ✅ Input validation using **DataAnnotations** and custom rules  
- ✅ Middleware for:
  - Global error handling (consistent JSON error responses)
  - Token-based authentication (`Bearer mysecrettoken123`)
  - Request/response logging  
- ✅ Pagination support for `GET /api/users`  
- ✅ In-memory data store (thread-safe) for demo purposes  

