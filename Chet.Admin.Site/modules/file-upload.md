# 文件上传

## 功能特性

- 本地存储（`uploads` 目录，GUID 命名）
- 上传 / 下载 / 删除
- 单文件最大 10MB
- 支持 15 种格式：`.jpg` `.jpeg` `.png` `.gif` `.pdf` `.doc` `.docx` `.xls` `.xlsx` 等
- 静态文件通过 `/uploads/{filename}` 访问
- 记录上传人、文件大小、MIME 类型

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| POST | `/files/upload` | 上传（multipart/form-data） |
| GET | `/files/{id}` | 文件信息 |
| GET | `/files/{id}/download` | 下载 |
| DELETE | `/files/{id}` | 删除 |

## 上传限制

- 单文件最大 10MB
- 支持格式：`.jpg` `.jpeg` `.png` `.gif` `.pdf` `.doc` `.docx` `.xls` `.xlsx` 等 15 种
- 静态文件通过 `/uploads/{filename}` 访问
