# Advanced URL shortener and analytics

## System Overview

This is a design for a URL shortener. A URL shortener is a tool that takes in a long, rough webpage address and returns a shorter, more compact, and manageable link as an output. This application takes in a long url and exposes a short url as well as analytics (such as the date and time of click, the device type the link was clicked, the city and country, the referral source.).

## Requirements

### Functional requirements

1. Create a short URL from a long URL.
2. Redirect traffic to the right webpage when they visit the short URL.
3. Optionally set expiry link of short URL.
4. Track each clicks (timestamp, IP address, user agent, referrer, country and city).
5. Query analytics per short URL (total clicks, clicks over time, top referrers, top countries)

### Non-functional requirements

1. The redirect latency should be low.
2. Analytics write should not block redirect (even though it happens before redirect).
3. The system should be able to handle read-heavy traffic (more read and redirects than write)
4. Short codes should be very unique.

## Capacity Estimate

1. URL creation per day >= 1000
2. Redirect per day >= 100000
3. Read to write ratio = 100:1
4. Storage per URL record = 2KB
5. Minimum total storage = 2 * 1000 = 2000KB / 2MB

## API Design

### Authentication Endpoints

1. POST /api/auth/register
    request_body: {
        email: string,
        password: string
    }

    response body on success(201)
    response_body: {
        status: success,
        data: {
            status: success,
            message: "Account created successfully, check email for account verification."
        }
    }

    response body on error(400, 422, 500)
    response_body: {
        status: error,
        detail: error detail
    }

2. POST /api/auth/login
    request_body: {
        email: string,
        password: string
    }

    response body on success (200)
    response_body: {
        status: success,
        data: {
            access_token: string,
            refresh_token: string,
            expires_at: datetime
            is_account_verified: boolean
        }
    }

    response body on error (400, 422, 500)
    response_body: {
        status: error,
        detail: error detail
    }

3. GET /api/auth/verify-account/{random-verification-string}
    response_body: {
        status: success,
        message: "Account verified successfully"
    }

4. POST /api/auth/forgot-password
    request_body: {
        email: string
    }
    response_body: {
        status: success,
        message: "An email has been sent to the provided account."
    }

5. POST /api/auth/change-password
    request_body: {
        new_password: string,
        confirm_new_password: string
    }

    response body on succeess
    response_body: {
        status: success,
        message: "Password reset successfully."
    }

    response body on error
    response_body: {
        status: error,
        detail: error detail
    }

6. POST /api/auth/logout
    response body on success
    response_body: {
        status: success,
        message: "suceessful logout"
    }

    response body on error
    response_body: {
        status: error,
        detail: error detail
    }

### Core endpoints

1. POST /api/v1/urls: create url
    required headers:  { Authorization: Bearer authentication_token }
    request body: {
        long_url: string
    }

    response body on success
    response body: {
        long_url: string,
        short_url: string
    }

    response body on error
    response_body: {
        status: error,
        detail: error detail
    }

2. GET /api/v1/urls: Get all urls for authorized individuals
    required headers: { Authorization: Bearer authentication_token }
    response body on success
    response_body: {
        status: success,
        data: []
    }

    response body on error
    response_body: {
        status: error,
        detail: error detail
    }

3. GET /api/v1/dashboard: view full analytics
    required headers: { Authorization: Bearer authentication_token }
    response body on success
    response_body: {
        status: success,
        data: {
            total_urls: int,
            total_clicks: int,
        }
    }

    response body on error
    response_body: {
        status: error,
        detail: error detail
    }

4. GET /api/v1/{id}/analytics
    required headers: { Authorization: Bearer authentication_token }
    response body on success
    response_body: {
        status: success,
        data: {
            total_clicks: int,
            total_regions: int,
        }
    }

    response body on error
    response_body: {
        status: error,
        detail: error detail
    }

### Status codes

- 200 OK for handled successful get requests.
- 201 Created for handled successful post requests.
- 422 Unprocessable Entity for validation errors.
- 400 Bad requests for structural errors.
- 500 Server Error for server errors.

## Database Schema

```mermaid
erDiagram
    USER {
        uuid id PK NOT NULL
        string username NOT NULL
        string email NOT NULL
        string password NOT NULL
        timestamptz created_at NOT NULL
        timestamptz updated_at NOT NULL
        bool is_active DEFAULT TRUE
        bool is_verified DEFAULT FALSE
    }
    URL {
        uuid id PK
        uuid user_id FK
        string original_url NOT NULL
        string code
        timestamptz createdAt NOT NULL
        timestamptz expiresAt
        bool is_active DEFAULT TRUE
    }
    CLICKS {
        uuid id PK
        string url_code FK
        timestamptz clicked_at
        string ip_address
        string geographic_location
        string referral_source
        string device_type
    }
    ANALYTICS {
        uuid id PK
        uuid user_id FK
        uuid url_id FK
        int total_clicks
    }
```

## Core Logic

### Short Code generation

The short code generation will make use of random characters of a fixed length of 8. Then it checks the database to make sure the character is not used. If the character is existing in the database, it regenerates another 8-length string. This reduces collision and prevents a long url from having the same short code.

### How clicks get written into DB

When a short url is requested for by an end-user, the click functionality is passed to a queue from which a background task will fetch from. On fetching the click details (via the url id) the background task runs the click function, populate the database and alert the frontend via a SSE.

## Architectural diagram

```mermaid
flowchart TD
    Client([Client]) --> API[API]
    API --> LB[Load Balancer]
    LB --> RL[Shared Rate Limiter]
    RL --> RLCheck{Is request limit exceeded?}
    RLCheck -->|yes| E429[Returns 429]
    E429 --> End1([End])
    RLCheck -->|no| Cache[Shared Cache]

    subgraph SI[Server Instance]
        Cache --> CacheCheck{Key exists in cache?}
        CacheCheck -->|yes| ReadCache[Read cached data]
        CacheCheck -->|no| QueryDB[Query database]

        ReadCache --> ExpCheck{Is short link expired?}
        QueryDB --> ExpCheck

        ExpCheck -->|yes| E410[Redirect to 410 Gone page]
        ExpCheck -->|no| ActiveCheck{Is link active?}

        ActiveCheck -->|no| EInactive[Redirect to inactive-link page]
        ActiveCheck -->|yes| Redirect[Redirect user to original URL]

        E410 --> End2([End])
        EInactive --> End2

        Redirect --> Analytics[Record analytics and click data]
        Redirect --> WriteCache[Write to cache if data was read from DB]
        Analytics -.-> End3([End])
        WriteCache -.-> End3
    end

    QueryDB --> DB[(Shared Database)]
```

## Tech Stack

- C#: Main backend logic
- Redis: Caching System
- PostgreSQL: Database
