/** Mirrors docs/API_CONVENTIONS.md — the binding API contract. */

export interface ApiErrorResponse {
  type: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface PaginatedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type MessRole = "Member" | "MealManager";
export type MemberStatus = "Pending" | "Active" | "Left" | "Rejected";

export interface Mess {
  id: string;
  name: string;
  createdAt: string;
  myRole: MessRole | "superAdmin";
  activeMemberCount: number;
}

export interface MessMember {
  memberId: string;
  name: string;
  phone: string;
  role: MessRole;
  status: MemberStatus;
  joinedAt: string;
}

export interface AuthUser {
  id: string;
  name: string;
  phone: string;
  globalRole: "user" | "superAdmin";
}

export interface AuthResult {
  accessToken: string;
  user: AuthUser;
}

export interface InviteResult {
  inviteToken: string;
  expiresAt: string;
}

export interface JoinedMessResult {
  messId: string;
  messName: string;
  status: string;
}
