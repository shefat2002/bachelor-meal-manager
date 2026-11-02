"use client";

import { apiClient } from "@/lib/api-client";
import type {
  AuthResult,
  InviteResult,
  JoinedMessResult,
  Mess,
  MessMember,
  PaginatedResult,
} from "@/types/api";

export const messesApi = {
  list: () =>
    apiClient
      .get<PaginatedResult<Mess>>("/api/messes", { params: { page: 1, pageSize: 50 } })
      .then((r) => r.data),

  get: (messId: string) => apiClient.get<Mess>(`/api/messes/${messId}`).then((r) => r.data),

  create: (name: string) => apiClient.post<Mess>("/api/messes", { name }).then((r) => r.data),

  join: (inviteToken: string) =>
    apiClient.post<JoinedMessResult>("/api/members/join", { inviteToken }).then((r) => r.data),

  invite: (messId: string) =>
    apiClient
      .post<InviteResult>("/api/members/invite", null, { headers: { "X-Mess-Id": messId } })
      .then((r) => r.data),

  listMembers: (messId: string) =>
    apiClient
      .get<PaginatedResult<MessMember>>("/api/members", {
        params: { page: 1, pageSize: 100 },
        headers: { "X-Mess-Id": messId },
      })
      .then((r) => r.data),

  approve: (messId: string, memberId: string) =>
    apiClient.put(`/api/members/${memberId}/approve`, null, { headers: { "X-Mess-Id": messId } }),

  reject: (messId: string, memberId: string) =>
    apiClient.put(`/api/members/${memberId}/reject`, null, { headers: { "X-Mess-Id": messId } }),

  updateRole: (messId: string, memberId: string, role: string) =>
    apiClient.put(`/api/members/${memberId}/role`, { role }, { headers: { "X-Mess-Id": messId } }),

  remove: (messId: string, memberId: string) =>
    apiClient.delete(`/api/members/${memberId}`, { headers: { "X-Mess-Id": messId } }),
};

export const authScreenApi = {
  register: (name: string, phone: string, password: string) =>
    apiClient.post<AuthResult>("/api/auth/register", { name, phone, password }).then((r) => r.data),

  login: (phone: string, password: string) =>
    apiClient.post<AuthResult>("/api/auth/login", { phone, password }).then((r) => r.data),

  requestOtp: (phone: string) =>
    apiClient
      .post<{ sent: boolean; devCode?: string; expiresAt: string }>("/api/auth/otp/request", {
        phone,
      })
      .then((r) => r.data),

  verifyOtp: (phone: string, code: string) =>
    apiClient.post<AuthResult>("/api/auth/otp/verify", { phone, code }).then((r) => r.data),

  logout: () => apiClient.post("/api/auth/logout", null, { withCredentials: true }),
};
