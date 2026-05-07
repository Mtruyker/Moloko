create table if not exists roles (
    id uuid primary key,
    name text not null unique
);

create table if not exists users (
    id uuid primary key,
    login text not null unique,
    password_hash text,
    full_name text not null,
    role text not null default 'Operator',
    role_id uuid references roles(id),
    is_active boolean not null default true,
    created_at timestamptz not null default now()
);

alter table if exists users
    add column if not exists role text not null default 'Operator';

create table if not exists animal_groups (
    id uuid primary key,
    name text not null,
    location text not null default ''
);

create table if not exists product_types (
    id uuid primary key,
    name text not null,
    shelf_life_hours integer not null
);

create table if not exists storage_tanks (
    id uuid primary key,
    name text not null,
    capacity_liters numeric(12, 3) not null check (capacity_liters > 0),
    location text not null default ''
);

create table if not exists customers (
    id uuid primary key,
    name text not null,
    inn text not null default '',
    address text not null default ''
);

create table if not exists vehicles (
    id uuid primary key,
    name text not null,
    plate_number text not null default '',
    driver text not null default ''
);

create table if not exists milk_yields (
    id uuid primary key,
    milked_at timestamptz not null,
    animal_group_id uuid not null references animal_groups(id),
    farm text not null,
    room text not null,
    operator_name text not null,
    volume_liters numeric(12, 3) not null check (volume_liters > 0),
    weight_kg numeric(12, 3) not null check (weight_kg > 0),
    temperature numeric(6, 2) not null,
    batch_id uuid
);

create table if not exists batches (
    id uuid primary key,
    batch_number text not null unique,
    created_at timestamptz not null,
    source_description text not null,
    volume_liters numeric(12, 3) not null check (volume_liters > 0),
    weight_kg numeric(12, 3) not null check (weight_kg > 0),
    remaining_liters numeric(12, 3) not null check (remaining_liters >= 0),
    status text not null,
    storage_tank_id uuid references storage_tanks(id),
    product_type_id uuid references product_types(id),
    expiration_date timestamptz not null,
    created_by text not null,
    documents text not null default '',
    notes text not null default ''
);

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'fk_milk_yields_batches'
    ) then
        alter table milk_yields
            add constraint fk_milk_yields_batches
            foreign key (batch_id) references batches(id);
    end if;
end $$;

create table if not exists batch_quality_tests (
    id uuid primary key,
    batch_id uuid not null references batches(id),
    test_date timestamptz not null,
    fat_percent numeric(6, 2) not null check (fat_percent > 0),
    protein_percent numeric(6, 2) not null check (protein_percent > 0),
    acidity numeric(6, 2) not null check (acidity > 0),
    density numeric(8, 2) not null check (density > 0),
    temperature numeric(6, 2) not null,
    visual_result text not null,
    has_foreign_impurities boolean not null default false,
    express_tests text not null default '',
    conclusion text not null,
    tested_by text not null,
    comment text not null default ''
);

create table if not exists quality_norms (
    id uuid primary key,
    product_type_id uuid references product_types(id),
    min_fat_percent numeric(6, 2) not null,
    min_protein_percent numeric(6, 2) not null,
    max_acidity numeric(6, 2) not null,
    min_density numeric(8, 2) not null,
    max_temperature numeric(6, 2) not null,
    valid_from date not null default current_date
);

create table if not exists stock_movements (
    id uuid primary key,
    batch_id uuid not null references batches(id),
    operation_type text not null,
    volume_liters numeric(12, 3) not null check (volume_liters > 0),
    from_tank_id uuid references storage_tanks(id),
    to_tank_id uuid references storage_tanks(id),
    operation_date timestamptz not null,
    responsible_user text not null,
    reason text not null default ''
);

create table if not exists shipments (
    id uuid primary key,
    shipment_number text not null unique,
    shipped_at timestamptz not null,
    customer_id uuid not null references customers(id),
    vehicle_id uuid not null references vehicles(id),
    temperature numeric(6, 2) not null,
    basis_document text not null,
    responsible_user text not null
);

create table if not exists shipment_items (
    id uuid primary key,
    shipment_id uuid not null references shipments(id),
    batch_id uuid not null references batches(id),
    volume_liters numeric(12, 3) not null check (volume_liters > 0)
);

create table if not exists documents (
    id uuid primary key,
    document_type text not null,
    number text not null,
    created_at timestamptz not null default now(),
    batch_id uuid references batches(id),
    shipment_id uuid references shipments(id),
    file_path text not null default ''
);

create table if not exists audit_log (
    id uuid primary key,
    created_at timestamptz not null default now(),
    user_name text not null,
    action text not null,
    details text not null default ''
);

create index if not exists ix_batches_status on batches(status);
create index if not exists ix_batches_created_at on batches(created_at);
create index if not exists ix_batches_expiration_date on batches(expiration_date);
create index if not exists ix_quality_tests_batch_id on batch_quality_tests(batch_id);
create index if not exists ix_stock_movements_batch_id on stock_movements(batch_id);
create index if not exists ix_shipments_shipped_at on shipments(shipped_at);
