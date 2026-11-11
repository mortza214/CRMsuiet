import { useEffect, useMemo, useState } from "react";
import {
  App,
  Button,
  Checkbox,
  Drawer,
  Empty,
  Input,
  Space,
  Tag,
  Typography,
} from "antd";
import { useUpdateRolePermissions } from "@/modules/settings/hooks/useSettings";
import type {
  AppRole,
  PermissionGroup,
} from "@/modules/settings/types";

interface RolePermissionsDrawerProps {
  open: boolean;
  role: AppRole | null;
  groups: PermissionGroup[];
  onClose: () => void;
}

const groupLabels: Record<string, string> = {
  Customers: "مشتریان",
  Catalog: "کاتالوگ سرویس‌ها",
  Orders: "سفارش‌ها",
  Billing: "فاکتورها",
  Payments: "پرداخت‌ها",
  Wallet: "کیف پول",
  Commission: "پورسانت",
  Tickets: "تیکت‌ها",
  Reports: "گزارش‌ها",
  Settings: "تنظیمات",
  Foundation: "زیرساخت",
  Forms: "فرم‌ها",
  Notifications: "اعلان‌ها",
  Identity: "کاربران و نقش‌ها",
};

export function RolePermissionsDrawer({
  open,
  role,
  groups,
  onClose,
}: RolePermissionsDrawerProps) {
  const { message } = App.useApp();
  const [checked, setChecked] = useState<string[]>([]);
  const [search, setSearch] = useState("");
  const updatePermissions = useUpdateRolePermissions();

  useEffect(() => {
    if (!open || !role) return;
    setChecked(role.permissions.map((permission) => permission.id));
    setSearch("");
  }, [open, role]);

  const filteredGroups = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return groups;

    return groups
      .map((group) => ({
        ...group,
        permissions: group.permissions.filter(
          (permission) =>
            permission.code.toLowerCase().includes(term) ||
            permission.displayName.toLowerCase().includes(term),
        ),
      }))
      .filter((group) => group.permissions.length > 0);
  }, [groups, search]);

  const save = async () => {
    if (!role) return;

    try {
      await updatePermissions.mutateAsync({
        roleId: role.id,
        permissionIds: checked,
      });
      message.success("دسترسی‌های نقش با موفقیت به‌روزرسانی شد.");
      onClose();
    } catch {
      // پیام خطا توسط NotificationProvider نمایش داده می‌شود.
    }
  };

  return (
    <Drawer
      title={`مدیریت دسترسی‌های ${role?.displayName ?? role?.name ?? ""}`}
      placement="left"
      width={640}
      open={open}
      onClose={onClose}
      footer={
        <Space className="drawer-actions">
          <Button onClick={onClose}>انصراف</Button>
          <Button
            type="primary"
            loading={updatePermissions.isPending}
            onClick={() => void save()}
          >
            ذخیره دسترسی‌ها
          </Button>
        </Space>
      }
    >
      <Space direction="vertical" size="middle" className="module-stack">
        <Input
          allowClear
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="جستجوی دسترسی..."
        />

        <Typography.Text type="secondary">
          {checked.length} دسترسی انتخاب شده است.
        </Typography.Text>

        <Checkbox.Group
          value={checked}
          onChange={(values) => setChecked(values as string[])}
          className="permission-groups"
        >
          {filteredGroups.map((group) => (
            <div className="permission-group" key={group.group}>
              <div className="permission-group-title">
                <Typography.Text strong>
                  {groupLabels[group.group] ?? group.group}
                </Typography.Text>
                <Tag bordered={false}>{group.permissions.length}</Tag>
              </div>
              <div className="permission-items">
                {group.permissions.map((permission) => (
                  <Checkbox value={permission.id} key={permission.id}>
                    <span>{permission.displayName}</span>
                    <small dir="ltr">{permission.code}</small>
                  </Checkbox>
                ))}
              </div>
            </div>
          ))}
        </Checkbox.Group>

        {filteredGroups.length === 0 && (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="دسترسی‌ای با این عبارت یافت نشد."
          />
        )}
      </Space>
    </Drawer>
  );
}
