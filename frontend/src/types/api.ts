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

// ---- Phase 2 domain contracts (mirror docs/API_CONVENTIONS.md §6) ----

export type DepositStatus = "Pending" | "Approved" | "Rejected";

export interface DailyMeal {
  id: string;
  memberId: string;
  date: string; // YYYY-MM-DD
  breakfast: number;
  lunch: number;
  dinner: number;
  totalUnits: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface BazarExpense {
  id: string;
  buyerMemberId: string;
  itemName: string;
  quantity: number;
  price: number; // unit price
  lineTotal: number;
  date: string;
  createdAt: string;
}

export interface UtilityType {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface UtilityPayment {
  id: string;
  utilityTypeId: string;
  utilityTypeName: string;
  amount: number;
  paymentDate: string;
  note: string | null;
  createdBy: string;
  createdAt: string;
}

export interface Deposit {
  id: string;
  memberId: string;
  memberName: string;
  amount: number;
  date: string;
  status: DepositStatus;
  approvedBy: string | null;
  approvedAt: string | null;
  note: string | null;
  createdAt: string;
}
