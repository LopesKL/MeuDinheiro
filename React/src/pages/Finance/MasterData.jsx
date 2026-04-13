import { useEffect, useState, useCallback, useMemo } from 'react';
import {
  Card,
  Table,
  Button,
  Modal,
  Form,
  Input,
  InputNumber,
  Switch,
  Select,
  DatePicker,
  Typography,
  Space,
  Popconfirm,
  Radio,
  Grid,
  Divider,
  Spin,
  ColorPicker,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  LeftOutlined,
  RightOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import { financeApi, EXPENSE_CREATION_SOURCE, normalizeCreditCardPayload } from '@services/financeApi';
import { countWeekdaysInMonth } from '@/utils/businessDays';
import CsvImportNubankModal from '@components/Finance/CsvImportNubankModal';

dayjs.locale('pt-br');
import { App } from 'antd';

const { Title, Text } = Typography;

const emptyGuid = '00000000-0000-0000-0000-000000000000';

/** API pode enviar camelCase ou PascalCase; Select precisa de id/name válidos. */
function normalizeFinanceCategory(raw) {
  if (!raw || typeof raw !== 'object') return null;
  const id = raw.id ?? raw.Id;
  if (id == null || id === '') return null;
  const name = String(raw.name ?? raw.Name ?? '').trim() || '—';
  const v = raw.isExpense ?? raw.IsExpense;
  const isExpense = v === true || v === 'true' || v === 1;
  return { ...raw, id, name, isExpense };
}

/** API pode enviar camelCase ou PascalCase; Select precisa de id/name estáveis. */
function normalizeFinanceCreditCard(raw) {
  if (!raw || typeof raw !== 'object') return null;
  const id = raw.id ?? raw.Id;
  if (id == null || id === '') return null;
  const name = String(raw.name ?? raw.Name ?? '').trim() || '—';
  return { ...raw, id, name };
}

/**
 * Itens da aba de lançamentos: gasto marcado como Lançamento rápido (creationSource=1)
 * ou legado (creationSource=0) que não é parcela, comprovante nem recorrente gerado pelo sistema.
 */
function isExpenseForQuickLaunchTab(e) {
  const src = Number(e.creationSource);
  if (src === EXPENSE_CREATION_SOURCE.QUICK_LAUNCH) return true;
  if (src !== EXPENSE_CREATION_SOURCE.UNSPECIFIED) return false;
  if (e.installmentPlanId) return false;
  if (e.imagePath) return false;
  const desc = ((e.description != null && String(e.description).trim()) || '').toLowerCase();
  if (desc.endsWith(' (recorrente)')) return false;
  return true;
}

const MASTER_SECTION_TABS = [
  { key: 'inc', label: 'Rendas' },
  //{ key: 'cc', label: 'Cartões' },
  //{ key: 'acc', label: 'Patrimônio' },
  // { key: 'debt', label: 'Dívidas' },
  { key: 'rec', label: 'Recorrentes' },
  { key: 'plan', label: 'Parcelamentos' },
  { key: 'mov', label: 'Lançamento rápido (mês)' },
  { key: 'cat', label: 'Categorias' },
];

function partitionIncomesByBatch(rows) {
  const standalone = [];
  const batchMap = new Map();
  for (const row of rows || []) {
    const bid = row.batchId;
    if (bid && bid !== emptyGuid) {
      if (!batchMap.has(bid)) batchMap.set(bid, []);
      batchMap.get(bid).push(row);
    } else {
      standalone.push(row);
    }
  }
  const groups = [...batchMap.entries()].map(([batchId, items]) => {
    const sorted = [...items].sort(
      (a, b) => new Date(a.referenceMonth) - new Date(b.referenceMonth)
    );
    const total = sorted.reduce((s, x) => s + Number(x.amount || 0), 0);
    return {
      batchId,
      items: sorted,
      total,
      source: sorted[0]?.source ?? '—',
      minM: sorted[0]?.referenceMonth,
      maxM: sorted[sorted.length - 1]?.referenceMonth,
    };
  });
  groups.sort((a, b) => new Date(b.minM) - new Date(a.minM));
  standalone.sort((a, b) => new Date(b.referenceMonth) - new Date(a.referenceMonth));
  return { incomeGroups: groups, incomeStandalone: standalone };
}

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

/** Gera dayjs para cada mês de start a end (inclusive), ambos no início do mês. */
function eachMonthInRange(start, end) {
  const months = [];
  let d = start.startOf('month');
  const last = end.startOf('month');
  while (d.isBefore(last) || d.isSame(last, 'month')) {
    months.push(d);
    d = d.add(1, 'month');
  }
  return months;
}

const mobileCardSx = {
  background: '#fff',
  borderRadius: 12,
  border: '1px solid #ececec',
  padding: 16,
  marginBottom: 12,
  boxShadow: '0 1px 3px rgba(0, 0, 0, 0.06)',
};

function CrudMobileHeader({ count, addLabel, onAdd, extra }) {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 16,
        gap: 12,
        flexWrap: 'wrap',
      }}
    >
      <Text style={{ fontSize: 15 }}>
        {count} {count === 1 ? 'registro' : 'registros'}
      </Text>
      <Space wrap>
        {extra}
        <Button type="primary" icon={<PlusOutlined />} onClick={onAdd} style={{ borderRadius: 8 }}>
          {addLabel}
        </Button>
      </Space>
    </div>
  );
}

/** Barra padrão (desktop): contagem + botão que abre modal de cadastro */
function TabActionBar({ count, addLabel, onAdd, extra }) {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 12,
        gap: 12,
        flexWrap: 'wrap',
      }}
    >
      <Text type="secondary">
        {count} {count === 1 ? 'registro' : 'registros'}
      </Text>
      <Space wrap>
        {extra}
        <Button type="primary" icon={<PlusOutlined />} onClick={onAdd}>
          {addLabel}
        </Button>
      </Space>
    </div>
  );
}

const MODAL_TYPE_LABELS = {
  cat: 'Categoria',
  cc: 'Cartão',
  acc: 'Conta',
  debt: 'Dívida',
  rec: 'Recorrente',
  inc: 'Renda',
};

function tabKeyForModalType(type) {
  const map = { cat: 'cat', cc: 'cc', acc: 'acc', debt: 'debt', rec: 'rec', inc: 'inc' };
  return map[type] || null;
}

function MobileCrudCard({ rows, onEdit, onDelete, deleteTitle, footer }) {
  return (
    <div style={mobileCardSx}>
      {rows.map((row, i) => (
        <div
          key={i}
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            gap: 12,
            marginBottom: i < rows.length - 1 ? 10 : 0,
          }}
        >
          <Text type="secondary" style={{ fontSize: 12, textTransform: 'uppercase', flexShrink: 0 }}>
            {row.label}
          </Text>
          <Text strong style={{ textAlign: 'right', wordBreak: 'break-word' }}>
            {row.value ?? '—'}
          </Text>
        </div>
      ))}
      <Divider style={{ margin: '14px 0 12px' }} />
      {footer ?? (
        <div style={{ display: 'flex', gap: 8 }}>
          <Button icon={<EditOutlined />} onClick={onEdit} style={{ flex: 1, borderRadius: 8 }}>
            Editar
          </Button>
          <div style={{ flex: 1 }}>
            <Popconfirm title={deleteTitle || 'Remover?'} onConfirm={onDelete}>
              <Button danger block icon={<DeleteOutlined />} style={{ borderRadius: 8 }}>
                Excluir
              </Button>
            </Popconfirm>
          </div>
        </div>
      )}
    </div>
  );
}

const MasterData = () => {
  const { message } = App.useApp();
  const screens = Grid.useBreakpoint();
  const isMobile = screens.md === false;
  const [active, setActive] = useState('inc');
  const [categories, setCategories] = useState([]);
  const [cards, setCards] = useState([]);
  const [accounts, setAccounts] = useState([]);
  const [debts, setDebts] = useState([]);
  const [recurring, setRecurring] = useState([]);
  const [plans, setPlans] = useState([]);
  const [incomes, setIncomes] = useState([]);
  const [tabLoading, setTabLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [launchMonth, setLaunchMonth] = useState(() => dayjs().startOf('month'));
  const [monthExpenses, setMonthExpenses] = useState([]);
  const [monthLaunchLoading, setMonthLaunchLoading] = useState(false);
  const [launchFetchTick, setLaunchFetchTick] = useState(0);
  const [planModalOpen, setPlanModalOpen] = useState(false);

  const [modal, setModal] = useState({ open: false, type: '', record: null });
  const [form] = Form.useForm();
  const modalCcKind = Form.useWatch('cardKind', form);
  const [planForm] = Form.useForm();
  const [recScheduleModal, setRecScheduleModal] = useState({ open: false, recurring: null });
  const [recScheduleForm] = Form.useForm();
  const incomeSpreadMode = Form.useWatch('incomeSpreadMode', form);

  const { incomeGroups, incomeStandalone } = useMemo(
    () => partitionIncomesByBatch(incomes),
    [incomes]
  );

  const loadTabData = useCallback(
    async (tabKey) => {
      setTabLoading(true);
      try {
        switch (tabKey) {
          case 'inc': {
            const [inc, cc] = await Promise.all([financeApi.incomes(), financeApi.creditCards()]);
            setIncomes(inc || []);
            setCards(cc || []);
            break;
          }
          case 'cat': {
            const c = await financeApi.categories();
            setCategories(c || []);
            break;
          }
          case 'rec': {
            const [r, c, cc] = await Promise.all([
              financeApi.recurring(),
              financeApi.categories(),
              financeApi.creditCards(),
            ]);
            setRecurring(r || []);
            setCategories(c || []);
            setCards(cc || []);
            break;
          }
          case 'plan': {
            const [p, c, cc] = await Promise.all([
              financeApi.installmentPlans(),
              financeApi.categories(),
              financeApi.creditCards(),
            ]);
            setPlans(p || []);
            setCategories(c || []);
            setCards(cc || []);
            break;
          }
          case 'cc': {
            const cc = await financeApi.creditCards();
            setCards(cc || []);
            break;
          }
          case 'acc': {
            const a = await financeApi.accounts();
            setAccounts(a || []);
            break;
          }
          case 'debt': {
            const d = await financeApi.debts();
            setDebts(d || []);
            break;
          }
          default:
            break;
        }
      } catch (e) {
        message.error(e.message || 'Erro ao carregar dados');
      } finally {
        setTabLoading(false);
      }
    },
    [message]
  );

  useEffect(() => {
    if (active === 'mov') return;
    loadTabData(active);
  }, [active, loadTabData]);

  /** Evita Select de categoria/cartão vazio se o modal abrir antes do loadTabData('rec') concluir. */
  useEffect(() => {
    if (!modal.open || modal.type !== 'rec') return undefined;
    const needCat = !categories?.length;
    const needCards = !cards?.length;
    if (!needCat && !needCards) return undefined;
    let cancelled = false;
    (async () => {
      try {
        const [c, cc] = await Promise.all([
          needCat ? financeApi.categories() : Promise.resolve(null),
          needCards ? financeApi.creditCards() : Promise.resolve(null),
        ]);
        if (cancelled) return;
        if (c) setCategories(c);
        if (cc) setCards(cc);
      } catch (e) {
        if (!cancelled) message.error(e.message || 'Erro ao carregar categorias ou cartões');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [modal.open, modal.type, categories?.length, cards?.length, message]);

  useEffect(() => {
    if (active !== 'mov') return undefined;
    let cancelled = false;
    (async () => {
      setMonthLaunchLoading(true);
      try {
        const y = launchMonth.year();
        const m = launchMonth.month() + 1;
        const list = await financeApi.expenses(y, m);
        if (!cancelled) setMonthExpenses(Array.isArray(list) ? list : []);
      } catch (e) {
        if (!cancelled) {
          message.error(e.message || 'Erro ao carregar lançamentos do mês');
          setMonthExpenses([]);
        }
      } finally {
        if (!cancelled) setMonthLaunchLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [active, launchMonth, launchFetchTick, message]);

  const launchMonthRows = useMemo(() => {
    const fromExpenses = (monthExpenses || [])
      .filter((e) => isExpenseForQuickLaunchTab(e))
      .map((e) => ({
        key: `exp-${e.id}`,
        expenseId: e.id,
        sortDate: dayjs(e.date),
        tipo: 'Despesa',
        titulo: (e.description && String(e.description).trim()) || e.categoryName || '—',
        subtitulo:
          e.description && e.categoryName && String(e.description).trim() !== e.categoryName
            ? e.categoryName
            : null,
        valor: Number(e.amount || 0),
      }));
    return fromExpenses.sort((a, b) => b.sortDate.valueOf() - a.sortDate.valueOf());
  }, [monthExpenses]);

  const launchMonthLabel = launchMonth.format('MMMM [de] YYYY');
  const canLaunchGoNext = launchMonth.isBefore(dayjs(), 'month');

  const openModal = (type, record = null) => {
    setModal({ open: true, type, record });
    form.resetFields();
    if (record) {
      const patch = { ...record };
      if (patch.dueDate) patch.dueDate = dayjs(patch.dueDate);
      if (type === 'cc') patch.cardKind = patch.isMealVoucher ? 'meal' : 'credit';
      form.setFieldsValue(patch);
    } else if (type === 'debt') {
      form.setFieldsValue({ id: emptyGuid, paidAmount: 0, totalAmount: 0 });
    } else if (type === 'rec') {
      form.setFieldsValue({ id: emptyGuid, type: 0, paymentMethod: 5, dayOfMonth: 1, active: true });
    } else if (type === 'acc') {
      form.setFieldsValue({ id: emptyGuid, type: 0, balance: 0, currency: 'BRL' });
    } else if (type === 'cc') {
      form.setFieldsValue({ id: emptyGuid, cardKind: 'credit', closingDay: 10, dueDay: 15 });
    } else if (type === 'cat') {
      form.setFieldsValue({ id: emptyGuid, isExpense: true });
    } else if (type === 'inc') {
      const yStart = dayjs().startOf('year');
      form.setFieldsValue({
        id: emptyGuid,
        incomeSpreadMode: 'single',
        referenceMonth: dayjs().startOf('month'),
        monthRange: [yStart, yStart.add(11, 'month')],
      });
    }
  };

  const submitModal = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      let successMsg = 'Salvo';
      if (modal.type === 'cat') {
        if (!v.id || v.id === emptyGuid) await financeApi.categoryCreate(v);
        else await financeApi.categoryUpdate(v.id, v);
      } else if (modal.type === 'cc') {
        await financeApi.creditCardUpsert(normalizeCreditCardPayload(v));
      } else if (modal.type === 'acc') {
        await financeApi.accountUpsert(v);
      } else if (modal.type === 'debt') {
        await financeApi.debtUpsert({ ...v, dueDate: v.dueDate?.toDate?.()?.toISOString() || v.dueDate });
      } else if (modal.type === 'rec') {
        await financeApi.recurringUpsert(v);
      } else if (modal.type === 'inc') {
        const isIncomeEdit = modal.record && modal.record.id !== emptyGuid;
        if (!isIncomeEdit && v.incomeSpreadMode === 'range') {
          const months = eachMonthInRange(v.monthRange[0], v.monthRange[1]);
          const batchId = crypto.randomUUID();
          const payload = {
            source: v.source,
            amount: v.amount,
            description: v.description,
            batchId,
            creditCardId: v.creditCardId || null,
          };
          for (const m of months) {
            await financeApi.incomeUpsert({
              id: emptyGuid,
              ...payload,
              referenceMonth: m.startOf('month').toDate().toISOString(),
            });
          }
          successMsg =
            months.length === 1 ? 'Salvo' : `${months.length} rendas cadastradas (uma por mês)`;
        } else {
          const rm = v.referenceMonth?.startOf?.('month')?.toDate?.() || new Date();
          await financeApi.incomeUpsert({
            ...v,
            referenceMonth: rm.toISOString(),
            batchId: v.batchId || modal.record?.batchId || null,
            creditCardId: v.creditCardId || null,
          });
        }
      }
      message.success(successMsg);
      setModal({ open: false, type: '', record: null });
      const tk = tabKeyForModalType(modal.type);
      if (tk) await loadTabData(tk);
    } catch (e) {
      if (e?.errorFields) throw e;
      message.error(e.message || 'Erro ao salvar');
    } finally {
      setSaving(false);
    }
  };

  const openPlanModal = () => {
    planForm.resetFields();
    setPlanModalOpen(true);
  };

  const submitPlan = async () => {
    setSaving(true);
    try {
      const v = await planForm.validateFields();
      await financeApi.installmentPlanCreate({
        id: emptyGuid,
        creditCardId: v.creditCardId || null,
        categoryId: v.categoryId,
        description: v.description,
        totalAmount: v.totalAmount,
        installmentCount: v.installmentCount,
        startDate: v.startDate?.toDate?.()?.toISOString() || v.startDate,
        installments: [],
      });
      message.success('Plano criado com parcelas');
      planForm.resetFields();
      setPlanModalOpen(false);
      await loadTabData('plan');
    } catch (e) {
      if (e?.errorFields) throw e;
      message.error(e.message || 'Erro ao criar plano');
    } finally {
      setSaving(false);
    }
  };

  const ensureCsvImportLookups = useCallback(async () => {
    const [c, cc] = await Promise.all([financeApi.categories(), financeApi.creditCards()]);
    setCategories(Array.isArray(c) ? c : []);
    setCards(Array.isArray(cc) ? cc : []);
  }, []);

  const catCols = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    { title: 'Despesa?', dataIndex: 'isExpense', key: 'isExpense', render: (x) => (x ? 'Sim' : 'Não') },
    {
      title: 'Ações',
      key: 'a',
      render: (_, r) => (
        <Space>
          <Button size="small" onClick={() => openModal('cat', r)}>
            Editar
          </Button>
          <Popconfirm title="Remover?" onConfirm={() => financeApi.categoryDelete(r.id).then(() => loadTabData('cat'))}>
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const ccCols = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    {
      title: 'Tipo',
      key: 'tipo',
      width: 130,
      render: (_, r) => (r.isMealVoucher ? 'Vale alimentação' : 'Crédito'),
    },
    {
      title: 'Fechamento',
      dataIndex: 'closingDay',
      key: 'closingDay',
      render: (v, r) => (r.isMealVoucher ? '—' : v),
    },
    {
      title: 'Vencimento',
      dataIndex: 'dueDay',
      key: 'dueDay',
      render: (v, r) => (r.isMealVoucher ? '—' : v),
    },
    {
      title: 'Ações',
      key: 'a',
      render: (_, r) => (
        <Space>
          <Button size="small" onClick={() => openModal('cc', r)}>
            Editar
          </Button>
          <Popconfirm title="Remover?" onConfirm={() => financeApi.creditCardDelete(r.id).then(() => loadTabData('cc'))}>
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const accCols = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    { title: 'Saldo', dataIndex: 'balance', key: 'balance', render: formatMoney },
    {
      title: 'Ações',
      key: 'a',
      render: (_, r) => (
        <Space>
          <Button size="small" onClick={() => openModal('acc', r)}>
            Editar
          </Button>
          <Popconfirm title="Remover?" onConfirm={() => financeApi.accountDelete(r.id).then(() => loadTabData('acc'))}>
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const debtCols = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    { title: 'Saldo', dataIndex: 'balance', key: 'balance', render: formatMoney },
    {
      title: 'Ações',
      key: 'a',
      render: (_, r) => (
        <Space>
          <Button size="small" onClick={() => openModal('debt', r)}>
            Editar
          </Button>
          <Popconfirm title="Remover?" onConfirm={() => financeApi.debtDelete(r.id).then(() => loadTabData('debt'))}>
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const recurringScheduleList = (r) => r?.amountSchedules ?? r?.AmountSchedules ?? [];

  const recurringEffectiveAmount = (r) => {
    const e = r?.effectiveAmount ?? r?.EffectiveAmount;
    if (e != null && e !== '') return Number(e);
    return Number(r?.amount ?? r?.Amount ?? 0);
  };

  const recCols = [
    { title: 'Descrição', dataIndex: 'description', key: 'description' },
    {
      title: 'Vigente (mês atual)',
      key: 'effectiveAmount',
      render: (_, r) => formatMoney(recurringEffectiveAmount(r)),
    },
    {
      title: 'Valor base',
      key: 'baseAmount',
      render: (_, r) => formatMoney(Number(r.amount ?? r.Amount ?? 0)),
    },
    { title: 'Dia', dataIndex: 'dayOfMonth', key: 'dayOfMonth' },
    {
      title: 'Cartão',
      key: 'recCc',
      width: 120,
      ellipsis: true,
      render: (_, r) => incomeCardLabel(r.creditCardId),
    },
    { title: 'Ativo', dataIndex: 'active', key: 'active', render: (x) => (x ? 'Sim' : 'Não') },
    {
      title: 'Ações',
      key: 'a',
      render: (_, r) => (
        <Space wrap>
          <Button size="small" onClick={() => openModal('rec', r)}>
            Editar
          </Button>
          <Button
            size="small"
            onClick={() => {
              setRecScheduleModal({ open: true, recurring: r });
              recScheduleForm.resetFields();
              recScheduleForm.setFieldsValue({ effectiveFrom: dayjs().startOf('month') });
            }}
          >
            Vigências
          </Button>
          <Popconfirm title="Remover?" onConfirm={() => financeApi.recurringDelete(r.id).then(() => loadTabData('rec'))}>
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const planCols = [
    { title: 'Descrição', dataIndex: 'description', key: 'description' },
    { title: 'Total', dataIndex: 'totalAmount', key: 'totalAmount', render: formatMoney },
    { title: 'Parcelas', dataIndex: 'installmentCount', key: 'installmentCount' },
    {
      title: 'Cartão',
      key: 'planCc',
      width: 130,
      ellipsis: true,
      render: (_, r) => incomeCardLabel(r.creditCardId),
    },
    {
      title: 'Ações',
      key: 'a',
      render: (_, r) => (
        <Popconfirm title="Excluir plano (sem parcelas pagas)?" onConfirm={() => financeApi.installmentPlanDelete(r.id).then(() => loadTabData('plan'))}>
          <Button size="small" danger>
            Excluir
          </Button>
        </Popconfirm>
      ),
    },
  ];

  const expenseCats = useMemo(() => {
    const norm = (categories || []).map(normalizeFinanceCategory).filter(Boolean);
    const onlyExpense = norm.filter((c) => c.isExpense);
    /* Se nada veio como despesa (flag errada no JSON ou só receitas), lista todas para não travar o import. */
    return onlyExpense.length > 0 ? onlyExpense : norm;
  }, [categories]);

  const creditCardsNormalized = useMemo(
    () => (cards || []).map(normalizeFinanceCreditCard).filter(Boolean),
    [cards]
  );

  const creditCardSelectOptions = useMemo(
    () => creditCardsNormalized.map((c) => ({ value: c.id, label: c.name })),
    [creditCardsNormalized]
  );

  const incomeCardLabel = useCallback((creditCardId) => {
    if (!creditCardId) return '—';
    const fid = String(creditCardId);
    const c = creditCardsNormalized.find((x) => String(x.id) === fid);
    return c?.name ?? '—';
  }, [creditCardsNormalized]);

  const planCsvImportExtra = (
    <CsvImportNubankModal
      expenseCats={expenseCats}
      cards={cards}
      onSyncLookups={ensureCsvImportLookups}
      onImported={async ({ anyPlan, anyRec }) => {
        if (anyPlan) await loadTabData('plan');
        if (anyRec) await loadTabData('rec');
      }}
    />
  );

  const isIncomeEdit = modal.type === 'inc' && modal.record;

  const launchCols = [
    {
      title: 'Data',
      key: 'sortDate',
      width: 108,
      render: (_, r) => r.sortDate.format('DD/MM/YYYY'),
    },
    {
      title: 'Tipo',
      dataIndex: 'tipo',
      key: 'tipo',
      width: 96,
      render: (t) => (
        <Text type={t === 'Renda' ? 'success' : 'secondary'} strong={t === 'Renda'}>
          {t}
        </Text>
      ),
    },
    {
      title: 'Descrição',
      key: 'titulo',
      ellipsis: true,
      render: (_, r) => (
        <span>
          {r.titulo}
          {r.subtitulo ? (
            <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
              {r.subtitulo}
            </Text>
          ) : null}
        </span>
      ),
    },
    {
      title: 'Valor',
      dataIndex: 'valor',
      key: 'valor',
      width: 120,
      align: 'right',
      render: (v) => formatMoney(v),
    },
    {
      title: 'Ações',
      key: 'movActions',
      width: 108,
      fixed: 'right',
      render: (_, r) => (
        <Popconfirm
          title="Excluir este gasto?"
          okText="Excluir"
          okButtonProps={{ danger: true }}
          onConfirm={async () => {
            try {
              await financeApi.expenseDelete(r.expenseId);
              message.success('Gasto removido');
              setLaunchFetchTick((t) => t + 1);
            } catch (e) {
              message.error(e.message || 'Erro ao remover');
            }
          }}
        >
          <Button danger size="small" icon={<DeleteOutlined />}>
            Excluir
          </Button>
        </Popconfirm>
      ),
    },
  ];

  const incomeCols = [
    { title: 'Fonte', dataIndex: 'source', key: 'source' },
    { title: 'Valor', dataIndex: 'amount', key: 'amount', render: formatMoney },
    {
      title: 'Mês ref.',
      dataIndex: 'referenceMonth',
      key: 'referenceMonth',
      render: (d) => (d ? dayjs(d).format('MM/YYYY') : '—'),
    },
    {
      title: 'Cartão',
      key: 'creditCard',
      width: 140,
      ellipsis: true,
      render: (_, r) => incomeCardLabel(r.creditCardId),
    },
    {
      title: 'Ações',
      key: 'ai',
      render: (_, r) => (
        <Space>
          <Button
            size="small"
            onClick={() => {
              openModal('inc', {
                ...r,
                referenceMonth: r.referenceMonth ? dayjs(r.referenceMonth) : dayjs(),
                batchId: r.batchId ?? undefined,
              });
            }}
          >
            Editar
          </Button>
          <Popconfirm
            title="Remover renda?"
            onConfirm={async () => {
              try {
                await financeApi.incomeDelete(r.id);
                await loadTabData('inc');
              } catch (e) {
                message.error(e.message);
              }
            }}
          >
            <Button size="small" danger>
              Excluir
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Title level={3} style={{ marginTop: 0 }}>
        Cadastros
      </Title>
      <Card>
        {isMobile ? (
          <Select
            value={active}
            onChange={setActive}
            options={MASTER_SECTION_TABS.map(({ key, label }) => ({ value: key, label }))}
            style={{ width: '100%', marginBottom: 16 }}
            size="large"
          />
        ) : (
          <Space wrap size="small" style={{ marginBottom: 16 }}>
            {MASTER_SECTION_TABS.map(({ key, label }) => (
              <Button
                key={key}
                type={active === key ? 'primary' : 'default'}
                onClick={() => setActive(key)}
                style={{
                  padding: '2px 10px',
                  border: 'none',
                  height: '32px',
                  backgroundColor: active === key ? '#000' : '#f5f5f5',
                }}
              >
                {label}
              </Button>
            ))}
          </Space>
        )}

        {active === 'mov' ? (
          <div>
            <Space
              wrap
              style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}
              align="center"
            >
              <Button icon={<LeftOutlined />} onClick={() => setLaunchMonth((m) => m.subtract(1, 'month').startOf('month'))}>
                Mês anterior
              </Button>
              <Text strong>{launchMonthLabel}</Text>
              <Button
                icon={<RightOutlined />}
                disabled={!canLaunchGoNext}
                onClick={() => setLaunchMonth((m) => m.add(1, 'month').startOf('month'))}
              >
                Próximo mês
              </Button>
            </Space>
            <div style={{ marginBottom: 12 }}>
              <Text type="secondary">
                {launchMonthRows.length}{' '}
                {launchMonthRows.length === 1 ? 'gasto' : 'gastos'} (lançamento rápido e registros manuais anteriores)
              </Text>
            </div>
            <Spin spinning={monthLaunchLoading}>
              {isMobile ? (
                launchMonthRows.length === 0 && !monthLaunchLoading ? (
                  <Text type="secondary">Nenhum gasto manual neste mês</Text>
                ) : (
                  launchMonthRows.map((r) => (
                    <div key={r.key} style={{ ...mobileCardSx, marginBottom: 12 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          {r.sortDate.format('DD/MM/YYYY')}
                        </Text>
                        <Text type={r.tipo === 'Renda' ? 'success' : 'secondary'} strong>
                          {r.tipo}
                        </Text>
                      </div>
                      <Text strong style={{ display: 'block', marginBottom: 4 }}>
                        {r.titulo}
                      </Text>
                      {r.subtitulo ? (
                        <Text type="secondary" style={{ fontSize: 13, display: 'block', marginBottom: 8 }}>
                          {r.subtitulo}
                        </Text>
                      ) : null}
                      <Text style={{ fontSize: 16 }}>{formatMoney(r.valor)}</Text>
                      <Divider style={{ margin: '14px 0 12px' }} />
                      <Popconfirm
                        title="Excluir este gasto?"
                        okText="Excluir"
                        okButtonProps={{ danger: true }}
                        onConfirm={async () => {
                          try {
                            await financeApi.expenseDelete(r.expenseId);
                            message.success('Gasto removido');
                            setLaunchFetchTick((t) => t + 1);
                          } catch (e) {
                            message.error(e.message || 'Erro ao remover');
                          }
                        }}
                      >
                        <Button danger block icon={<DeleteOutlined />} style={{ borderRadius: 8 }}>
                          Excluir
                        </Button>
                      </Popconfirm>
                    </div>
                  ))
                )
              ) : (
                <Table
                  rowKey="key"
                  columns={launchCols}
                  dataSource={launchMonthRows}
                  pagination={false}
                  scroll={{ x: true }}
                  locale={{ emptyText: 'Nenhum gasto manual neste mês' }}
                />
              )}
            </Spin>
          </div>
        ) : (
          <Spin spinning={tabLoading}>
        {active === 'inc' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader count={incomes.length} addLabel="Nova renda" onAdd={() => openModal('inc')} />
                {incomeGroups.map((g) => (
                  <div key={g.batchId} style={{ marginBottom: 16 }}>
                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'flex-start',
                        gap: 8,
                        marginBottom: 10,
                        flexWrap: 'wrap',
                      }}
                    >
                      <div>
                        <Text strong>{g.source}</Text>
                        <div>
                          <Text type="secondary" style={{ fontSize: 12 }}>
                            {g.minM && g.maxM
                              ? `${dayjs(g.minM).format('MM/YYYY')} – ${dayjs(g.maxM).format('MM/YYYY')} · ${g.items.length} meses`
                              : `${g.items.length} meses`}{' '}
                            · {formatMoney(g.total)}
                          </Text>
                        </div>
                      </div>
                      <Popconfirm
                        title="Remover todas as rendas deste grupo?"
                        onConfirm={async () => {
                          try {
                            setTabLoading(true);
                            await Promise.all(g.items.map((x) => financeApi.incomeDelete(x.id)));
                            message.success('Grupo removido');
                            await loadTabData('inc');
                          } catch (e) {
                            message.error(e.message || 'Erro ao remover');
                          } finally {
                            setTabLoading(false);
                          }
                        }}
                      >
                        <Button size="small" danger style={{ borderRadius: 8 }}>
                          Excluir grupo
                        </Button>
                      </Popconfirm>
                    </div>
                    {g.items.map((r) => (
                      <MobileCrudCard
                        key={r.id}
                        rows={[
                          { label: 'Fonte', value: r.source },
                          { label: 'Valor', value: formatMoney(r.amount) },
                          {
                            label: 'Mês ref.',
                            value: r.referenceMonth ? dayjs(r.referenceMonth).format('MM/YYYY') : '—',
                          },
                          { label: 'Cartão', value: incomeCardLabel(r.creditCardId) },
                        ]}
                        onEdit={() =>
                          openModal('inc', {
                            ...r,
                            referenceMonth: r.referenceMonth ? dayjs(r.referenceMonth) : dayjs(),
                            batchId: r.batchId ?? undefined,
                          })
                        }
                        onDelete={async () => {
                          try {
                            await financeApi.incomeDelete(r.id);
                            await loadTabData('inc');
                          } catch (e) {
                            message.error(e.message);
                          }
                        }}
                        deleteTitle="Remover renda?"
                      />
                    ))}
                  </div>
                ))}
                {incomeStandalone.length > 0 && (
                  <>
                    {incomeGroups.length > 0 && (
                      <Title level={5} style={{ margin: '16px 0 8px' }}>
                        Rendas avulsas
                      </Title>
                    )}
                    {incomeStandalone.map((r) => (
                      <MobileCrudCard
                        key={r.id}
                        rows={[
                          { label: 'Fonte', value: r.source },
                          { label: 'Valor', value: formatMoney(r.amount) },
                          {
                            label: 'Mês ref.',
                            value: r.referenceMonth ? dayjs(r.referenceMonth).format('MM/YYYY') : '—',
                          },
                          { label: 'Cartão', value: incomeCardLabel(r.creditCardId) },
                        ]}
                        onEdit={() =>
                          openModal('inc', {
                            ...r,
                            referenceMonth: r.referenceMonth ? dayjs(r.referenceMonth) : dayjs(),
                            batchId: r.batchId ?? undefined,
                          })
                        }
                        onDelete={async () => {
                          try {
                            await financeApi.incomeDelete(r.id);
                            await loadTabData('inc');
                          } catch (e) {
                            message.error(e.message);
                          }
                        }}
                        deleteTitle="Remover renda?"
                      />
                    ))}
                  </>
                )}
                {incomes.length === 0 && <Text type="secondary">Nenhuma renda cadastrada.</Text>}
              </>
            ) : (
              <>
                <TabActionBar
                  count={incomes.length}
                  addLabel="Nova renda"
                  onAdd={() => openModal('inc')}
                />
                {incomeGroups.map((g) => (
                  <Card
                    key={g.batchId}
                    size="small"
                    style={{ marginBottom: 12 }}
                    title={
                      <Space wrap size="middle">
                        <Text strong>{g.source}</Text>
                        <Text type="secondary">
                          {g.minM && g.maxM
                            ? `${dayjs(g.minM).format('MM/YYYY')} – ${dayjs(g.maxM).format('MM/YYYY')} · ${g.items.length} meses`
                            : `${g.items.length} meses`}
                        </Text>
                        <Text>{formatMoney(g.total)} no período</Text>
                      </Space>
                    }
                    extra={
                      <Popconfirm
                        title="Remover todas as rendas deste grupo?"
                        onConfirm={async () => {
                          try {
                            setTabLoading(true);
                            await Promise.all(g.items.map((x) => financeApi.incomeDelete(x.id)));
                            message.success('Grupo removido');
                            await loadTabData('inc');
                          } catch (e) {
                            message.error(e.message || 'Erro ao remover');
                          } finally {
                            setTabLoading(false);
                          }
                        }}
                      >
                        <Button size="small" danger>
                          Excluir grupo
                        </Button>
                      </Popconfirm>
                    }
                  >
                    <Table
                      rowKey="id"
                      columns={incomeCols}
                      dataSource={g.items}
                      pagination={false}
                      size="small"
                      scroll={{ x: true }}
                    />
                  </Card>
                ))}
                {incomeStandalone.length > 0 && (
                  <>
                    {incomeGroups.length > 0 && (
                      <Title level={5} style={{ marginTop: incomeGroups.length ? 8 : 0 }}>
                        Rendas avulsas
                      </Title>
                    )}
                    <Table
                      rowKey="id"
                      columns={incomeCols}
                      dataSource={incomeStandalone}
                      pagination={false}
                      scroll={{ x: true }}
                    />
                  </>
                )}
                {incomeGroups.length === 0 && incomeStandalone.length === 0 && (
                  <Table rowKey="id" columns={incomeCols} dataSource={[]} pagination={false} />
                )}
              </>
            )}
          </div>
        )}

        {active === 'cat' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader
                  count={categories.length}
                  addLabel="Nova categoria"
                  onAdd={() => openModal('cat')}
                />
                {categories.map((r) => (
                  <MobileCrudCard
                    key={r.id}
                    rows={[
                      { label: 'Nome', value: r.name },
                      { label: 'Despesa?', value: r.isExpense ? 'Sim' : 'Não' },
                    ]}
                    onEdit={() => openModal('cat', r)}
                    onDelete={() => financeApi.categoryDelete(r.id).then(() => loadTabData('cat'))}
                    deleteTitle="Remover?"
                  />
                ))}
              </>
            ) : (
              <>
                <TabActionBar
                  count={categories.length}
                  addLabel="Nova categoria"
                  onAdd={() => openModal('cat')}
                />
                <Table rowKey="id" columns={catCols} dataSource={categories} pagination={false} scroll={{ x: true }} />
              </>
            )}
          </div>
        )}

        {active === 'cc' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader count={cards.length} addLabel="Novo cartão" onAdd={() => openModal('cc')} />
                {cards.map((r) => (
                  <MobileCrudCard
                    key={r.id}
                    rows={[
                      { label: 'Nome', value: r.name },
                      {
                        label: 'Tipo',
                        value: r.isMealVoucher ? 'Vale alimentação' : 'Crédito',
                      },
                      ...(r.isMealVoucher
                        ? [
                            { label: 'Valor/dia útil', value: formatMoney(r.mealVoucherDailyAmount) },
                            { label: 'Dia crédito', value: r.mealVoucherCreditDay },
                          ]
                        : [
                            { label: 'Fechamento', value: r.closingDay },
                            { label: 'Vencimento', value: r.dueDay },
                          ]),
                    ]}
                    onEdit={() => openModal('cc', r)}
                    onDelete={() => financeApi.creditCardDelete(r.id).then(() => loadTabData('cc'))}
                    deleteTitle="Remover?"
                  />
                ))}
              </>
            ) : (
              <>
                <TabActionBar count={cards.length} addLabel="Novo cartão" onAdd={() => openModal('cc')} />
                <Table rowKey="id" columns={ccCols} dataSource={cards} pagination={false} scroll={{ x: true }} />
              </>
            )}
          </div>
        )}

        {active === 'acc' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader count={accounts.length} addLabel="Nova conta" onAdd={() => openModal('acc')} />
                {accounts.map((r) => (
                  <MobileCrudCard
                    key={r.id}
                    rows={[
                      { label: 'Nome', value: r.name },
                      { label: 'Saldo', value: formatMoney(r.balance) },
                    ]}
                    onEdit={() => openModal('acc', r)}
                    onDelete={() => financeApi.accountDelete(r.id).then(() => loadTabData('acc'))}
                    deleteTitle="Remover?"
                  />
                ))}
              </>
            ) : (
              <>
                <TabActionBar count={accounts.length} addLabel="Nova conta" onAdd={() => openModal('acc')} />
                <Table rowKey="id" columns={accCols} dataSource={accounts} pagination={false} scroll={{ x: true }} />
              </>
            )}
          </div>
        )}

        {active === 'debt' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader count={debts.length} addLabel="Nova dívida" onAdd={() => openModal('debt')} />
                {debts.map((r) => (
                  <MobileCrudCard
                    key={r.id}
                    rows={[
                      { label: 'Nome', value: r.name },
                      { label: 'Saldo', value: formatMoney(r.balance) },
                    ]}
                    onEdit={() => openModal('debt', r)}
                    onDelete={() => financeApi.debtDelete(r.id).then(() => loadTabData('debt'))}
                    deleteTitle="Remover?"
                  />
                ))}
              </>
            ) : (
              <>
                <TabActionBar count={debts.length} addLabel="Nova dívida" onAdd={() => openModal('debt')} />
                <Table rowKey="id" columns={debtCols} dataSource={debts} pagination={false} scroll={{ x: true }} />
              </>
            )}
          </div>
        )}

        {active === 'rec' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader
                  count={recurring.length}
                  addLabel="Novo recorrente"
                  onAdd={() => openModal('rec')}
                />
                {recurring.map((r) => (
                  <MobileCrudCard
                    key={r.id}
                    rows={[
                      { label: 'Descrição', value: r.description },
                      { label: 'Vigente (mês atual)', value: formatMoney(recurringEffectiveAmount(r)) },
                      { label: 'Valor base', value: formatMoney(Number(r.amount ?? r.Amount ?? 0)) },
                      { label: 'Dia', value: r.dayOfMonth },
                      { label: 'Cartão', value: incomeCardLabel(r.creditCardId) },
                      { label: 'Ativo', value: r.active ? 'Sim' : 'Não' },
                    ]}
                    footer={
                      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                        <Button icon={<EditOutlined />} onClick={() => openModal('rec', r)} block style={{ borderRadius: 8 }}>
                          Editar
                        </Button>
                        <Button
                          block
                          style={{ borderRadius: 8 }}
                          onClick={() => {
                            setRecScheduleModal({ open: true, recurring: r });
                            recScheduleForm.resetFields();
                            recScheduleForm.setFieldsValue({ effectiveFrom: dayjs().startOf('month') });
                          }}
                        >
                          Vigências
                        </Button>
                        <Popconfirm title="Remover?" onConfirm={() => financeApi.recurringDelete(r.id).then(() => loadTabData('rec'))}>
                          <Button danger block icon={<DeleteOutlined />} style={{ borderRadius: 8 }}>
                            Excluir
                          </Button>
                        </Popconfirm>
                      </div>
                    }
                  />
                ))}
              </>
            ) : (
              <>
                <TabActionBar
                  count={recurring.length}
                  addLabel="Novo recorrente"
                  onAdd={() => openModal('rec')}
                />
                <Table rowKey="id" columns={recCols} dataSource={recurring} pagination={false} scroll={{ x: true }} />
              </>
            )}
          </div>
        )}

        {active === 'plan' && (
          <div>
            {isMobile ? (
              <>
                <CrudMobileHeader
                  count={plans.length}
                  addLabel="Novo parcelamento"
                  onAdd={openPlanModal}
                  extra={planCsvImportExtra}
                />
                {plans.map((r) => (
                  <MobileCrudCard
                    key={r.id}
                    rows={[
                      { label: 'Descrição', value: r.description },
                      { label: 'Total', value: formatMoney(r.totalAmount) },
                      { label: 'Parcelas', value: r.installmentCount },
                      { label: 'Cartão', value: incomeCardLabel(r.creditCardId) },
                    ]}
                    footer={
                      <Popconfirm
                        title="Excluir plano (sem parcelas pagas)?"
                        onConfirm={() => financeApi.installmentPlanDelete(r.id).then(() => loadTabData('plan'))}
                      >
                        <Button danger block icon={<DeleteOutlined />} style={{ borderRadius: 8 }}>
                          Excluir
                        </Button>
                      </Popconfirm>
                    }
                  />
                ))}
              </>
            ) : (
              <>
                <TabActionBar
                  count={plans.length}
                  addLabel="Novo parcelamento"
                  onAdd={openPlanModal}
                  extra={planCsvImportExtra}
                />
                <Table rowKey="id" columns={planCols} dataSource={plans} pagination={false} scroll={{ x: true }} />
              </>
            )}
          </div>
        )}
          </Spin>
        )}
      </Card>

      <Modal
        open={planModalOpen}
        title="Novo parcelamento"
        okText="Gerar parcelas"
        onCancel={() => setPlanModalOpen(false)}
        onOk={() => submitPlan()}
        confirmLoading={saving}
        destroyOnClose
        width={520}
      >
        <Form form={planForm} layout="vertical">
          <Form.Item name="description" label="Descrição" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="categoryId" label="Categoria" rules={[{ required: true }]}>
            <Select options={expenseCats.map((c) => ({ value: c.id, label: c.name }))} showSearch optionFilterProp="label" />
          </Form.Item>
          <Form.Item
            name="creditCardId"
            label="Cartão de crédito (opcional)"
            extra="Parcelas geradas serão lançadas como gastos neste cartão, quando informado."
          >
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Nenhum"
              options={creditCardSelectOptions}
            />
          </Form.Item>
          <Form.Item name="totalAmount" label="Valor total" rules={[{ required: true }]}>
            <InputNumber min={0.01} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="installmentCount" label="Nº parcelas" rules={[{ required: true }]}>
            <InputNumber min={1} max={120} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="startDate" label="1ª parcela" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={modal.open}
        title={modal.record ? `Editar ${MODAL_TYPE_LABELS[modal.type] || 'item'}` : `Nova ${MODAL_TYPE_LABELS[modal.type] || 'entrada'}`}
        onCancel={() => setModal({ open: false, type: '', record: null })}
        onOk={submitModal}
        confirmLoading={saving}
        destroyOnClose
        width={520}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="id" hidden>
            <Input />
          </Form.Item>
          {modal.type === 'cat' && (
            <>
              <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item name="isExpense" label="É despesa?" valuePropName="checked">
                <Switch />
              </Form.Item>
            </>
          )}
          {modal.type === 'cc' && (
            <>
              <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item name="cardKind" label="Tipo" rules={[{ required: true }]}>
                <Select
                  options={[
                    { value: 'credit', label: 'Cartão de crédito' },
                    { value: 'meal', label: 'Vale alimentação' },
                  ]}
                />
              </Form.Item>
              {modalCcKind === 'meal' ? (
                <>
                  <Form.Item
                    name="mealVoucherDailyAmount"
                    label="Valor fixo por dia útil"
                    rules={[
                      { required: true, message: 'Informe o valor por dia útil' },
                      { type: 'number', min: 0.01, message: 'Valor deve ser maior que zero' },
                    ]}
                  >
                    <InputNumber prefix="R$" min={0.01} style={{ width: '100%' }} controls={false} />
                  </Form.Item>
                  <Form.Item
                    name="mealVoucherCreditDay"
                    label="Dia do crédito no mês"
                    rules={[{ required: true, message: 'Informe o dia (1–31)' }]}
                  >
                    <InputNumber min={1} max={31} style={{ width: '100%' }} />
                  </Form.Item>
                  <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                    Crédito mensal estimado = valor × dias úteis (seg–sex) do mês. Ex.:{' '}
                    {countWeekdaysInMonth(dayjs().year(), dayjs().month() + 1)} dias úteis neste mês.
                  </Text>
                </>
              ) : (
                <>
                  <Form.Item name="closingDay" label="Dia fechamento" rules={[{ required: true }]}>
                    <InputNumber min={1} max={31} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="dueDay" label="Dia vencimento" rules={[{ required: true }]}>
                    <InputNumber min={1} max={31} style={{ width: '100%' }} />
                  </Form.Item>
                </>
              )}
              <Form.Item
                name="themeColor"
                label="Cor de identificação"
                extra="Usada no dashboard ao listar e filtrar cartões (#RGB ou #RRGGBB)."
                getValueFromEvent={(val) => {
                  if (val == null) return undefined;
                  if (typeof val.toHexString === 'function') return val.toHexString();
                  return val;
                }}
              >
                <ColorPicker showText format="hex" allowClear style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}
          {modal.type === 'acc' && (
            <>
              <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
                <Select options={[
                  { value: 0, label: 'Banco' },
                  { value: 1, label: 'Investimento' },
                  { value: 2, label: 'Cripto' },
                  { value: 3, label: 'Outro' },
                ]} />
              </Form.Item>
              <Form.Item name="balance" label="Saldo" rules={[{ required: true }]}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="currency" label="Moeda">
                <Input />
              </Form.Item>
            </>
          )}
          {modal.type === 'debt' && (
            <>
              <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item name="totalAmount" label="Valor total" rules={[{ required: true }]}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="paidAmount" label="Já pago" rules={[{ required: true }]}>
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="dueDate" label="Vencimento">
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
              <Form.Item name="monthlyPayment" label="Parcela mensal (opcional)">
                <InputNumber style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}
          {modal.type === 'inc' && (
            <>
              <Form.Item name="batchId" hidden>
                <Input />
              </Form.Item>
              <Form.Item name="source" label="Fonte" rules={[{ required: true }]}>
                <Input placeholder="Salário, aluguel, freelance..." />
              </Form.Item>
              <Form.Item name="amount" label="Valor" rules={[{ required: true }]}>
                <InputNumber min={0.01} style={{ width: '100%' }} />
              </Form.Item>
              {!isIncomeEdit && (
                <Form.Item name="incomeSpreadMode" label="Período" rules={[{ required: true }]}>
                  <Radio.Group>
                    <Radio.Button value="single">Um mês</Radio.Button>
                    <Radio.Button value="range">Intervalo (repetir cada mês)</Radio.Button>
                  </Radio.Group>
                </Form.Item>
              )}
              {isIncomeEdit || incomeSpreadMode !== 'range' ? (
                <Form.Item name="referenceMonth" label="Mês de referência" rules={[{ required: true }]}>
                  <DatePicker picker="month" style={{ width: '100%' }} format="MM/YYYY" />
                </Form.Item>
              ) : (
                <Form.Item
                  name="monthRange"
                  label="Intervalo de meses"
                  extra="Ex.: jan/2025 a dez/2025 para lançar o mesmo valor em todos os meses do ano."
                  rules={[
                    { required: true, message: 'Selecione o intervalo' },
                    {
                      validator(_, value) {
                        if (!value?.[0] || !value?.[1]) {
                          return Promise.reject(new Error('Selecione o mês inicial e o final'));
                        }
                        if (value[1].isBefore(value[0], 'month')) {
                          return Promise.reject(new Error('O mês final deve ser igual ou posterior ao inicial'));
                        }
                        return Promise.resolve();
                      },
                    },
                  ]}
                >
                  <DatePicker.RangePicker
                    picker="month"
                    style={{ width: '100%' }}
                    format="MM/YYYY"
                    placeholder={['Mês inicial', 'Mês final']}
                  />
                </Form.Item>
              )}
              <Form.Item name="description" label="Descrição">
                <Input />
              </Form.Item>
              <Form.Item
                name="creditCardId"
                label="Cartão de crédito (opcional)"
                extra="Útil para vincular renda a um cartão (ex.: cashback, estorno na fatura)."
              >
                <Select
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  placeholder="Nenhum"
                  options={creditCardSelectOptions}
                />
              </Form.Item>
            </>
          )}
          {modal.type === 'rec' && (
            <>
              <Form.Item name="description" label="Descrição" rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item name="type" label="Tipo" rules={[{ required: true }]}>
                <Select options={[
                  { value: 0, label: 'Fixo' },
                  { value: 1, label: 'Variável' },
                ]} />
              </Form.Item>
              <Form.Item
                name="amount"
                label="Valor base"
                rules={[{ required: true }]}
              >
                <InputNumber min={0.01} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="categoryId" label="Categoria" rules={[{ required: true }]}>
                <Select options={expenseCats.map((c) => ({ value: c.id, label: c.name }))} showSearch optionFilterProp="label" />
              </Form.Item>

              <Form.Item
                name="creditCardId"
                label="Cartão de crédito (opcional)"
                extra="Se pagamento for no crédito, associe ao cartão cadastrado."
              >
                <Select
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  placeholder="Nenhum"
                  options={creditCardSelectOptions}
                />
              </Form.Item>
              <Form.Item name="dayOfMonth" label="Dia do mês" rules={[{ required: true }]}>
                <InputNumber min={1} max={31} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="active" label="Ativo" valuePropName="checked">
                <Switch />
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>

      <Modal
        title={
          recScheduleModal.recurring
            ? `Vigências — ${(recScheduleModal.recurring.description || '').trim() || 'Recorrente'}`
            : 'Vigências'
        }
        open={recScheduleModal.open}
        onCancel={() => {
          setRecScheduleModal({ open: false, recurring: null });
          recScheduleForm.resetFields();
        }}
        footer={null}
        destroyOnClose
        width={520}
      >
        <Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
          Cada vigência define o valor a partir do 1º dia do mês escolhido. O mês atual e os futuros usam a vigência mais
          recente; meses anteriores ao primeiro ajuste seguem o valor base.
        </Text>
        {recurringScheduleList(recScheduleModal.recurring).length > 0 && (
          <Table
            size="small"
            pagination={false}
            style={{ marginBottom: 16 }}
            rowKey={(row) => row.id ?? row.Id}
            dataSource={recurringScheduleList(recScheduleModal.recurring)}
            columns={[
              {
                title: 'A partir de',
                dataIndex: 'effectiveFrom',
                key: 'effectiveFrom',
                render: (d) => (d ? dayjs(d).format('MM/YYYY') : '—'),
              },
              {
                title: 'Valor',
                dataIndex: 'amount',
                key: 'amount',
                render: (a) => formatMoney(Number(a ?? 0)),
              },
              {
                title: '',
                key: 'del',
                width: 88,
                render: (_, row) => (
                  <Popconfirm
                    title="Remover esta vigência?"
                    onConfirm={async () => {
                      const rid = recScheduleModal.recurring.id ?? recScheduleModal.recurring.Id;
                      const sid = row.id ?? row.Id;
                      try {
                        await financeApi.recurringAmountScheduleDelete(rid, sid);
                        message.success('Vigência removida');
                        await loadTabData('rec');
                        const updated = (await financeApi.recurring()) || [];
                        const next = updated.find((x) => (x.id ?? x.Id) === rid);
                        setRecScheduleModal((s) => ({ ...s, recurring: next || null }));
                      } catch (e) {
                        message.error(e.message || 'Erro ao remover');
                      }
                    }}
                  >
                    <Button size="small" danger type="link">
                      Remover
                    </Button>
                  </Popconfirm>
                ),
              },
            ]}
          />
        )}
        <Divider plain>Nova vigência</Divider>
        <Form
          form={recScheduleForm}
          layout="vertical"
          onFinish={async (v) => {
            const rid = recScheduleModal.recurring?.id ?? recScheduleModal.recurring?.Id;
            if (!rid) return;
            try {
              await financeApi.recurringAmountSchedule(rid, {
                effectiveFrom: v.effectiveFrom.startOf('month').toDate().toISOString(),
                amount: v.amount,
              });
              message.success('Vigência salva');
              recScheduleForm.resetFields();
              recScheduleForm.setFieldsValue({ effectiveFrom: dayjs().startOf('month') });
              await loadTabData('rec');
              const updated = (await financeApi.recurring()) || [];
              const next = updated.find((x) => (x.id ?? x.Id) === rid);
              setRecScheduleModal((s) => ({ ...s, recurring: next || s.recurring }));
            } catch (e) {
              message.error(e.message || 'Erro ao salvar');
            }
          }}
        >
          <Form.Item name="effectiveFrom" label="Válido a partir de (mês)" rules={[{ required: true }]}>
            <DatePicker picker="month" style={{ width: '100%' }} format="MM/YYYY" />
          </Form.Item>
          <Form.Item name="amount" label="Valor neste período" rules={[{ required: true }]}>
            <InputNumber min={0.01} style={{ width: '100%' }} />
          </Form.Item>
          <Button type="primary" htmlType="submit">
            Salvar vigência
          </Button>
        </Form>
      </Modal>
    </div>
  );
};

export default MasterData;
