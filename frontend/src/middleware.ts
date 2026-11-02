import { NextResponse, type NextRequest } from "next/server";

/**
 * Route protection (skill bmm-rbac-route-guards): authentication only — the presence of the
 * HttpOnly bmm_refresh cookie (set by /api/auth/*). Fine-grained role checks happen in
 * MessProvider + <Authorize>; the backend remains the enforcement authority.
 */
export function middleware(request: NextRequest) {
  if (!request.cookies.has("bmm_refresh")) {
    const url = request.nextUrl.clone();
    url.pathname = "/login";
    url.search = `next=${encodeURIComponent(request.nextUrl.pathname)}`;
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/dashboard/:path*", "/invite/:path*"],
};
