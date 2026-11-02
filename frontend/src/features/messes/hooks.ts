"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { messesApi } from "@/features/messes/services";

/** Query keys per skill bmm-server-state — ['messes', messId, ...] hierarchy. */
export const messKeys = {
  all: ["messes"] as const,
  detail: (messId: string) => ["messes", messId] as const,
  members: (messId: string) => ["messes", messId, "members"] as const,
};

export function useMesses() {
  return useQuery({ queryKey: messKeys.all, queryFn: messesApi.list });
}

export function useMess(messId: string) {
  return useQuery({ queryKey: messKeys.detail(messId), queryFn: () => messesApi.get(messId) });
}

export function useMembers(messId: string) {
  return useQuery({
    queryKey: messKeys.members(messId),
    queryFn: () => messesApi.listMembers(messId),
  });
}

export function useCreateMess() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => messesApi.create(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: messKeys.all }),
  });
}

export function useJoinMess() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (inviteToken: string) => messesApi.join(inviteToken),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: messKeys.all }),
  });
}

function useInvalidateMembers(messId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: messKeys.members(messId) });
    queryClient.invalidateQueries({ queryKey: messKeys.all });
  };
}

export function useCreateInvite(messId: string) {
  return useMutation({ mutationFn: () => messesApi.invite(messId) });
}

export function useApproveMember(messId: string) {
  const invalidate = useInvalidateMembers(messId);
  return useMutation({
    mutationFn: (memberId: string) => messesApi.approve(messId, memberId).then(() => undefined),
    onSuccess: invalidate,
  });
}

export function useRejectMember(messId: string) {
  const invalidate = useInvalidateMembers(messId);
  return useMutation({
    mutationFn: (memberId: string) => messesApi.reject(messId, memberId).then(() => undefined),
    onSuccess: invalidate,
  });
}

export function useUpdateMemberRole(messId: string) {
  const invalidate = useInvalidateMembers(messId);
  return useMutation({
    mutationFn: ({ memberId, role }: { memberId: string; role: string }) =>
      messesApi.updateRole(messId, memberId, role).then(() => undefined),
    onSuccess: invalidate,
  });
}

export function useRemoveMember(messId: string) {
  const invalidate = useInvalidateMembers(messId);
  return useMutation({
    mutationFn: (memberId: string) => messesApi.remove(messId, memberId).then(() => undefined),
    onSuccess: invalidate,
  });
}
