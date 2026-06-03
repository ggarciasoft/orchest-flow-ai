SELECT "TenantId", "Key", LEFT("Value", 20) as "ValuePreview" FROM "PlatformSettings" WHERE "Key" LIKE 'gmail%';
