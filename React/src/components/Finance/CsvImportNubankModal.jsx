import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Modal,
  Table,
  Button,
  Input,
  InputNumber,
  Select,
  DatePicker,
  Typography,
  Space,
  Checkbox,
  Radio,
  Tag,
  App,
} from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import { financeApi } from '@services/financeApi';
import {
  parseNubankCsvExport,
  csvRowImportDedupKey,
  normalizeImportDedupText,
} from '@/utils/nubankCsvImport';

const { Text } = Typography;

const emptyGuid = '00000000-0000-0000-0000-000000000000';

const csvImportSelectProps = {
  virtual: false,
  popupMatchSelectWidth: false,
  getPopupContainer: () => document.body,
  styles: { popup: { root: { zIndex: 3100, minWidth: 220 } } },
};

const csvImportDatePickerPopup = {
  getPopupContainer: () => document.body,
  popupStyle: { zIndex: 3100 },
};

const recurringPaymentOptions = [
  { value: 0, label: 'Dinheiro' },
  { value: 1, label: 'Débito' },
  { value: 2, label: 'Crédito' },
  { value: 3, label: 'Pix' },
  { value: 4, label: 'Transferência' },
  { value: 5, label: 'Outro' },
];

/** Nome editável sem disparar setState no pai a cada tecla — evita re-render da tabela inteira. */
const CsvImportNameCell = memo(function CsvImportNameCell({ row, dupInBatch, draftsRef }) {
  const [v, setV] = useState(row.displayName);

  /* Só reseta o campo quando a linha (id) muda — evita reset ao re-renderizar a linha por outros campos. */
  useEffect(() => {
    setV(row.displayName);
    draftsRef.current[row.id] = row.displayName;
  }, [row.id, row.displayName]);

  const orig = (row.originalName ?? row.displayName ?? '').trim() || '—';

  return (
    <div>
      <Input
        size="small"
        value={v}
        onChange={(e) => {
          const n = e.target.value;
          setV(n);
          draftsRef.current[row.id] = n;
        }}
      />
      <Text type="secondary" style={{ fontSize: 11, display: 'block', marginTop: 4, lineHeight: 1.35 }}>
        Nome original (CSV): {orig}
      </Text>
      {dupInBatch ? (
        <Tag color="error" style={{ marginTop: 4 }}>
          Mesma data, valor e nome original — duplicata
        </Tag>
      ) : null}
    </div>
  );
});

/**
 * Importação CSV Nubank: botão + input file + modal.
 * Estado e colunas ficam aqui para não re-renderizar MasterData a cada edição.
 */
function CsvImportNubankModal({ expenseCats, cards, onSyncLookups, onImported }) {
  const { message } = App.useApp();
  const fileInputRef = useRef(null);
  const displayNameDraftsRef = useRef({});

  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [rows, setRows] = useState([]);
  const [defaultCategoryId, setDefaultCategoryId] = useState(null);
  const [defaultCreditCardId, setDefaultCreditCardId] = useState(null);

  const updateRow = useCallback((id, patch) => {
    setRows((prev) => prev.map((r) => (r.id === id ? { ...r, ...patch } : r)));
  }, []);

  const openPicker = () => fileInputRef.current?.click();

  const onFileChange = async (e) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    try {
      if (onSyncLookups) await onSyncLookups();
      const text = await file.text();
      const parsed = parseNubankCsvExport(text);
      if (!parsed.length) {
        message.warning('Nenhuma linha válida no CSV');
        return;
      }
      const next = {};
      for (const r of parsed) {
        next[r.id] = r.displayName;
      }
      displayNameDraftsRef.current = next;
      setRows(parsed);
      setOpen(true);
    } catch (err) {
      message.error(err.message || 'Não foi possível ler o CSV');
    }
  };

  const applyDefaultsToSelected = () => {
    setRows((prev) =>
      prev.map((r) => {
        if (!r.selected) return r;
        return {
          ...r,
          ...(defaultCategoryId ? { categoryId: defaultCategoryId } : {}),
          ...(defaultCreditCardId != null
            ? {
                creditCardId: defaultCreditCardId || null,
                paymentMethod: defaultCreditCardId ? 2 : r.paymentMethod,
              }
            : {}),
        };
      })
    );
    message.success('Padrões aplicados às linhas marcadas');
  };

  const toggleAllSelected = (selected) => {
    setRows((prev) => prev.map((r) => ({ ...r, selected })));
  };

  const dupKeyCounts = useMemo(() => {
    const m = new Map();
    for (const r of rows) {
      if (!r.selected) continue;
      const k = csvRowImportDedupKey(r);
      m.set(k, (m.get(k) || 0) + 1);
    }
    return m;
  }, [rows]);

  const columns = useMemo(
    () => [
      {
        title: '✓',
        key: 'sel',
        width: 40,
        render: (_, r) => (
          <Checkbox
            checked={r.selected}
            onChange={(e) => updateRow(r.id, { selected: e.target.checked })}
          />
        ),
      },
      {
        title: 'Tipo',
        key: 'kind',
        width: 200,
        render: (_, r) => (
          <Radio.Group
            size="small"
            value={r.kind}
            onChange={(e) => updateRow(r.id, { kind: e.target.value })}
          >
            <Radio.Button value="plan" disabled={!r.installmentTotal}>
              Parcelamento
            </Radio.Button>
            <Radio.Button value="recurring">Recorrente</Radio.Button>
          </Radio.Group>
        ),
      },
      {
        title: 'Nome',
        key: 'name',
        width: 196,
        render: (_, r) => {
          const dk = csvRowImportDedupKey(r);
          const dupInBatch = r.selected && (dupKeyCounts.get(dk) || 0) > 1;
          return (
            <CsvImportNameCell key={r.id} row={r} dupInBatch={dupInBatch} draftsRef={displayNameDraftsRef} />
          );
        },
      },
      {
        title: 'Data (CSV)',
        key: 'd',
        width: 104,
        render: (_, r) => (r.rowDate?.isValid?.() ? r.rowDate.format('DD/MM/YYYY') : '—'),
      },
      {
        title: 'Valor linha',
        key: 'amt',
        width: 112,
        render: (_, r) => (
          <InputNumber
            size="small"
            min={0.01}
            step={0.01}
            value={r.amount}
            onChange={(v) => {
              const amt = v ?? 0;
              updateRow(r.id, {
                amount: amt,
                totalPlanAmount:
                  r.installmentTotal && r.kind === 'plan'
                    ? Math.round(amt * r.installmentTotal * 100) / 100
                    : r.totalPlanAmount,
              });
            }}
            style={{ width: '100%' }}
          />
        ),
      },
      {
        title: 'Plano / recorrente',
        key: 'extra',
        width: 232,
        render: (_, r) =>
          r.kind === 'plan' ? (
            <Space direction="vertical" size={4} style={{ width: '100%' }}>
              <Text type="secondary" style={{ fontSize: 12 }}>
                {r.installmentCurrent}/{r.installmentTotal} · 1ª parcela (estimada pela fatura)
              </Text>
              <DatePicker
                {...csvImportDatePickerPopup}
                size="small"
                style={{ width: '100%' }}
                format="DD/MM/YYYY"
                value={r.planStartDate}
                onChange={(d) => updateRow(r.id, { planStartDate: d })}
              />
              <div>
                <Text type="secondary" style={{ fontSize: 11, display: 'block', marginBottom: 2 }}>
                  Valor total do plano
                </Text>
                <InputNumber
                  size="small"
                  min={0.01}
                  value={r.totalPlanAmount}
                  onChange={(v) => updateRow(r.id, { totalPlanAmount: v ?? 0 })}
                  style={{ width: '100%' }}
                />
              </div>
            </Space>
          ) : (
            <Space direction="vertical" size={4} style={{ width: '100%' }}>
              <div>
                <Text type="secondary" style={{ fontSize: 11, display: 'block', marginBottom: 2 }}>
                  Dia do mês
                </Text>
                <InputNumber
                  size="small"
                  min={1}
                  max={31}
                  value={r.dayOfMonth}
                  onChange={(v) => updateRow(r.id, { dayOfMonth: v ?? 1 })}
                  style={{ width: '100%' }}
                />
              </div>
              {!r.creditCardId ? (
                <Select
                  {...csvImportSelectProps}
                  size="small"
                  style={{ width: '100%' }}
                  options={recurringPaymentOptions}
                  value={r.paymentMethod ?? 5}
                  onChange={(v) => updateRow(r.id, { paymentMethod: v })}
                />
              ) : (
                <Text type="secondary" style={{ fontSize: 12 }}>
                  Pagamento: crédito (cartão abaixo)
                </Text>
              )}
            </Space>
          ),
      },
      {
        title: 'Categoria',
        key: 'cat',
        width: 148,
        render: (_, r) => (
          <Select
            {...csvImportSelectProps}
            size="small"
            style={{ width: '100%' }}
            showSearch
            optionFilterProp="label"
            filterOption={(input, opt) =>
              String(opt?.label ?? '')
                .toLowerCase()
                .includes(String(input).toLowerCase())
            }
            placeholder="Obrigatório"
            value={r.categoryId ?? null}
            onChange={(v) => updateRow(r.id, { categoryId: v ?? null })}
            options={expenseCats.map((c) => ({ value: c.id, label: c.name }))}
            notFoundContent={
              expenseCats.length ? 'Nenhuma opção' : 'Sem categorias — cadastre na aba Categorias'
            }
          />
        ),
      },
      {
        title: 'Cartão',
        key: 'cc',
        width: 128,
        render: (_, r) => (
          <Select
            {...csvImportSelectProps}
            size="small"
            allowClear
            showSearch
            optionFilterProp="label"
            style={{ width: '100%' }}
            placeholder="Opcional"
            value={r.creditCardId ?? null}
            onChange={(v) =>
              updateRow(r.id, {
                creditCardId: v ?? null,
                paymentMethod: v ? 2 : r.paymentMethod ?? 5,
              })
            }
            options={cards.map((c) => ({
              value: c.id ?? c.Id,
              label: c.name ?? c.Name ?? '—',
            }))}
          />
        ),
      },
    ],
    [cards, dupKeyCounts, expenseCats, updateRow]
  );

  const mergeDisplayNames = useCallback(
    (list) =>
      list.map((r) => ({
        ...r,
        displayName: (displayNameDraftsRef.current[r.id] ?? r.displayName ?? '').trim() || r.displayName,
      })),
    []
  );

  const submit = async () => {
    const merged = mergeDisplayNames(rows.filter((r) => r.selected));
    if (!merged.length) {
      message.warning('Marque ao menos uma linha para importar');
      return;
    }
    for (const r of merged) {
      if (!r.categoryId) {
        message.error(`Defina categoria em todos os itens (ex.: "${r.displayName || 'sem nome'}")`);
        return;
      }
      if (r.kind === 'plan') {
        if (!r.installmentTotal || !r.planStartDate) {
          message.error(`Parcelamento incompleto: "${r.displayName}"`);
          return;
        }
        const total = r.totalPlanAmount ?? r.amount * r.installmentTotal;
        if (!(total > 0)) {
          message.error(`Valor total inválido: "${r.displayName}"`);
          return;
        }
      } else if (!r.dayOfMonth || r.dayOfMonth < 1 || r.dayOfMonth > 31) {
        message.error(`Dia do mês inválido: "${r.displayName}"`);
        return;
      }
    }

    const seenKeys = new Map();
    for (const r of merged) {
      const key = csvRowImportDedupKey(r);
      if (seenKeys.has(key)) {
        const orig = r.originalName ?? r.displayName ?? '—';
        message.error(
          `Há mais de uma linha selecionada com o mesmo nome original, data e valor do CSV (duplicata no lote). Ex.: "${orig}"`
        );
        return;
      }
      seenKeys.set(key, r.id);
    }

    let freshPlans;
    let freshRecurring;
    try {
      [freshPlans, freshRecurring] = await Promise.all([
        financeApi.installmentPlans(),
        financeApi.recurring(),
      ]);
    } catch (e) {
      message.error(e.message || 'Erro ao verificar cadastros existentes');
      return;
    }

    const existingNormNames = new Set(
      [...(freshPlans || []), ...(freshRecurring || [])].map((x) =>
        normalizeImportDedupText(x.description)
      )
    );
    for (const r of merged) {
      const on = normalizeImportDedupText(r.originalName ?? r.displayName);
      if (on && existingNormNames.has(on)) {
        message.error(
          `Já existe parcelamento ou recorrente com o mesmo nome original do CSV: "${r.originalName ?? r.displayName}". Remova a linha ou altere o cadastro existente.`
        );
        return;
      }
    }

    setSaving(true);
    let anyRec = false;
    let anyPlan = false;
    try {
      for (const r of merged) {
        if (r.kind === 'plan') {
          const totalAmount = r.totalPlanAmount ?? r.amount * r.installmentTotal;
          await financeApi.installmentPlanCreate({
            id: emptyGuid,
            creditCardId: r.creditCardId || null,
            categoryId: r.categoryId || null,
            description: (r.displayName || '').trim() || 'Parcelamento',
            totalAmount,
            installmentCount: r.installmentTotal,
            startDate: r.planStartDate.startOf('day').toDate().toISOString(),
            installments: [],
          });
          anyPlan = true;
        } else {
          await financeApi.recurringUpsert({
            id: emptyGuid,
            type: r.recurringType ?? 0,
            amount: r.amount,
            categoryId: r.categoryId,
            description: (r.displayName || '').trim() || 'Recorrente',
            paymentMethod: r.creditCardId ? 2 : r.paymentMethod ?? 5,
            dayOfMonth: r.dayOfMonth,
            active: r.active !== false,
            creditCardId: r.creditCardId || null,
          });
          anyRec = true;
        }
      }
      message.success(`${merged.length} item(ns) importado(s)`);
      setOpen(false);
      setRows([]);
      displayNameDraftsRef.current = {};
      onImported?.({ anyPlan, anyRec });
    } catch (err) {
      message.error(err.message || 'Erro ao importar');
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setOpen(false);
    setRows([]);
    displayNameDraftsRef.current = {};
  };

  const selectedCount = rows.filter((r) => r.selected).length;

  return (
    <>
      <input
        ref={fileInputRef}
        type="file"
        accept=".csv,text/csv"
        style={{ display: 'none' }}
        onChange={onFileChange}
      />
      <Button icon={<UploadOutlined />} onClick={openPicker}>
        Importar CSV
      </Button>

      <Modal
        open={open}
        title="Importar CSV (Nubank)"
        okText={`Importar ${selectedCount} selecionado(s)`}
        onCancel={handleCancel}
        onOk={submit}
        confirmLoading={saving}
        destroyOnClose
        centered
        width="max-content"
        styles={{
          content: {
            maxWidth: 'calc(80vw - 24px)',
            minWidth: 280,
          },
          body: {
            maxHeight: '75vh',
            overflowY: 'auto',
            overflowX: 'auto',
            padding: '12px 16px',
          },
        }}
      >
        <Text type="secondary" style={{ display: 'block', marginBottom: 12, maxWidth: 640, lineHeight: 1.5 }}>
          O <strong>nome original (CSV)</strong> é fixo (texto após remover &quot;Parcela X/Y&quot;). Você pode editar o
          nome que será salvo; a importação bloqueia duplicatas: mesma data + valor + nome original entre linhas
          selecionadas, ou se já existir parcelamento/recorrente com descrição igual ao nome original (comparação sem
          diferenciar maiúsculas).
        </Text>
        <Space wrap style={{ marginBottom: 12 }}>
          <Select
            {...csvImportSelectProps}
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="Categoria padrão"
            style={{ minWidth: 200 }}
            value={defaultCategoryId}
            onChange={(v) => setDefaultCategoryId(v ?? null)}
            options={expenseCats.map((c) => ({ value: c.id, label: c.name }))}
          />
          <Select
            {...csvImportSelectProps}
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="Cartão padrão (opcional)"
            style={{ minWidth: 200 }}
            value={defaultCreditCardId}
            onChange={(v) => setDefaultCreditCardId(v ?? null)}
            options={cards.map((c) => ({ value: c.id, label: c.name }))}
          />
          <Button onClick={applyDefaultsToSelected}>Aplicar padrões às linhas marcadas</Button>
          <Button onClick={() => toggleAllSelected(true)}>Marcar todos</Button>
          <Button onClick={() => toggleAllSelected(false)}>Desmarcar todos</Button>
        </Space>
        <Table
          size="small"
          rowKey="id"
          dataSource={rows}
          columns={columns}
          pagination={false}
          tableLayout="auto"
          style={{ width: 'max-content', maxWidth: '100%' }}
        />
      </Modal>
    </>
  );
}

export default CsvImportNubankModal;
